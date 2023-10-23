
using Astroid.Core.Cache;
using Astroid.Core;
using Astroid.Entity;
using Astroid.Providers;
using Microsoft.EntityFrameworkCore;
using Astroid.Entity.Extentions;
using Astroid.Core.MessageQueue;
using Astroid.Core.Notification;

namespace Astroid.BackgroundServices.Order;

public class NotificationTracker : IHostedService
{
	private AQNotification NotificationQueue { get; set; }
	private ILogger<NotificationTracker> Logger { get; set; }
	private IServiceProvider Services { get; set; }
	private IConfiguration Configuration { get; set; }

	public NotificationTracker(AQNotification notificationQueue, IConfiguration configuration, ILogger<NotificationTracker> logger, IServiceProvider services)
	{
		NotificationQueue = notificationQueue;
		Logger = logger;
		Services = services;
		Configuration = configuration;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		await NotificationQueue.Setup(cancellationToken);
		await NotificationQueue.Subscribe(SendNotification, cancellationToken);

		Logger.LogInformation("[Notification Service] Started notification service.");
		_ = Task.Run(() => DoJob(cancellationToken), cancellationToken);
	}

	public async Task DoJob(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			var scope = Services.CreateScope();
			using var db = scope.ServiceProvider.GetRequiredService<AstroidDb>();
			var pendingNotifications = db.Notifications.Where(x => x.Status == NotificationStatus.Pending && (x.ExpireDate == DateTime.MinValue || x.ExpireDate > DateTime.UtcNow)).ToList();

			foreach (var notification in pendingNotifications)
			{
				await NotificationQueue.Publish(new AQONotificationMessage { OrderId = notification.Id }, cancellationToken);
				notification.Status = NotificationStatus.Processing;
				await db.SaveChangesAsync(cancellationToken);
			}

			await Task.Delay(60 * 1000, cancellationToken);
		}
	}

	private async Task SendNotification(AQONotificationMessage msg, CancellationToken cancellationToken)
	{
		Logger.LogInformation($" Notification {msg.OrderId} started to be sent.");
		using var scope = Services.CreateScope();
		using var db = scope.ServiceProvider.GetRequiredService<AstroidDb>();

		var notification = await db.Notifications.FirstOrDefaultAsync(x => x.Id == msg.OrderId, cancellationToken);
		if (notification == null)
		{
			Logger.LogError($"Notification {msg.OrderId} not found.");
			return;
		}

		if (notification.IsExpired)
		{
			notification.Status = NotificationStatus.Expired;
			await db.SaveChangesAsync(cancellationToken);
			return;
		}

		try
		{
			var notificator = new Notificator(Configuration, notification.Channel);
			var to = await GetRecipient(db, notification, cancellationToken);
			if (string.IsNullOrEmpty(to)) throw new Exception($"Recipient not found for notification {notification.Id}.");

			var result = await notificator.SendNotifications(notification.Subject, notification.Content, to);
			if (result.Success) notification.Status = NotificationStatus.Sent;
			else
			{
				notification.Status = NotificationStatus.Failed;
				notification.Error = result.Message;
			}
		}
		catch (Exception ex)
		{
			notification.Status = NotificationStatus.Failed;
			notification.Error = ex.Message;
			Logger.LogError(ex, $"Notification {notification.Id} could not be sent.");
		}

		await db.SaveChangesAsync(cancellationToken);
	}

	private async Task<string?> GetRecipient(AstroidDb db, ADNotification notification, CancellationToken cancellationToken)
	{
		var user = await db.Users.FirstOrDefaultAsync(x => x.Id == notification.UserId, cancellationToken) ?? throw new Exception($"User {notification.UserId} not found.");

		return notification.Channel switch
		{
			ChannelType.Mail => user.Email,
			ChannelType.Sms => user.Phone,
			ChannelType.Telegram => user.TelegramId,
			_ => throw new Exception($"Channel {notification.Channel} not supported."),
		};
	}

	public Task StopAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}

