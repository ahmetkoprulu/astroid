using Astroid.Core;
using Astroid.Core.Cache;
using Astroid.Core.Notification;
using Astroid.Entity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Astroid.Services;
public class ASNotification : IHostedService
{
	private readonly ILogger<ASNotification> Logger;
	private readonly IServiceScopeFactory ScopeFactory;
	private readonly ICacheService Cache;

	public ASNotification(IServiceScopeFactory scopeFactory, InMemoryCache cache, ILogger<ASNotification> logger)
	{
		ScopeFactory = scopeFactory;
		Cache = cache;
		Logger = logger;

	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		Logger.LogInformation("Notification Service Started.");
		_ = Task.Run(() => DoJob(cancellationToken), cancellationToken);
	}

	private async Task DoJob(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				await SendPendingNotifications(cancellationToken);
			}
			catch (Exception ex)
			{
				Logger.LogCritical(ex, "Notification Error!");
			}
			finally
			{
				await WaitForIt(20, cancellationToken);
			}
		}
	}

	public async Task SendPendingNotifications(CancellationToken cancellationToken)
	{
		using var scope = ScopeFactory.CreateScope();
		using var db = scope.ServiceProvider.GetRequiredService<AstroidDb>();
		var notifications = db.Notifications.AsNoTracking().Where(x => x.Status == NotificationStatus.Pending);

		foreach (var notification in notifications) await ForEachNotification(db, notification, cancellationToken);
	}

	public async Task ForEachNotification(AstroidDb db, ADNotification notification, CancellationToken cancellationToken)
	{
		try
		{
			var channel = GetChannel(notification);
			var to = GetRecipient(notification);
			notification.To = to;
			channel.Send(notification.Subject, notification.Content, to);
			notification.Status = NotificationStatus.Sent;
		}
		catch (Exception ex)
		{
			notification.Status = NotificationStatus.Failed;
			notification.Error = ex.Message;
		}
		finally
		{
			notification.SentDate = DateTime.UtcNow;
			db.Update(notification);
			await db.SaveChangesAsync(cancellationToken);
		}

	}

	public INotificationChannel GetChannel(ADNotification notification)
	{
		switch (notification.Channel)
		{
			case ChannelType.Mail:
				return new MailChannel();
			default:
				throw new Exception("Invalid notification channel type.");
		}
	}

	public string GetRecipient(ADNotification notification)
	{
		switch (notification.Channel)
		{
			case ChannelType.Mail:
				return notification.User.Email;
			default:
				throw new Exception("Invalid notification channel type.");
		}
	}

	private static async Task WaitForIt(int seconds = 10, CancellationToken cancellationToken = default) =>
		await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);

	public Task StopAsync(CancellationToken cancellationToken)
	{
		Logger.LogInformation("Notification Service Stopped.");
		return Task.CompletedTask;
	}
}
