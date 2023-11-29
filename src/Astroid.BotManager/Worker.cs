
using Astroid.Core.Cache;
using Astroid.Core;
using Astroid.Entity;
using Astroid.Providers;
using Microsoft.EntityFrameworkCore;
using Astroid.Entity.Extentions;
using Astroid.Core.MessageQueue;

namespace Astroid.BotManager;

public class Worker : IHostedService
{
	public Guid Id { get; } = Guid.Empty;
	private IServiceProvider ServiceProvider { get; set; }
	private ICacheService Cache { get; set; }
	private IMessageQueue Mq { get; set; }
	private ILogger<Worker> Logger { get; set; }
	// private AstroidDb Db { get; set; }
	private BotRegistrationManager BotManager { get; set; }

	public Worker(IConfiguration config, IServiceProvider serviceProvider, BotRegistrationManager botManager, ICacheService cache, IMessageQueue mq, ILogger<Worker> logger)
	{
		Id = GetManagerId(config);
		if (Id == Guid.Empty) throw new Exception("Invalid Bot Manager Id.");

		ServiceProvider = serviceProvider;
		BotManager = botManager;
		Cache = cache;
		Mq = mq;
		Logger = logger;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		Logger.LogInformation("Starting Bot Manager Service.");
		await BotManager.Setup(cancellationToken);
		await BotManager.Subscribe(async (msg, ct) => await ExecuteOrder(ServiceProvider, msg, ct), cancellationToken);
		await BotManager.Subscribe(async (msg, ct) => await ExecuteOrder(ServiceProvider, msg, ct), cancellationToken);
		Logger.LogInformation("Subscribed registration queues.");

		_ = Task.Run(() => Ping(cancellationToken), cancellationToken);
	}


	public async Task Ping(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			await CreateOrPing(cancellationToken);
			await Task.Delay(1000 * 60 * 5, cancellationToken);
		}
	}

	public async Task CreateOrPing(CancellationToken cancellationToken = default)
	{
		var scope = ServiceProvider.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AstroidDb>();

		var manager = await db.BotManagers.FirstOrDefaultAsync(x => x.Id == Id, cancellationToken);

		if (manager == null)
			await db.BotManagers.AddAsync(new() { Id = Id, CreatedDate = DateTime.UtcNow, PingDate = DateTime.UtcNow }, cancellationToken);
		else
			manager.PingDate = DateTime.UtcNow;

		await db.SaveChangesAsync(cancellationToken);
	}

	public async Task ExecuteOrder(IServiceProvider sp, AQOrderMessage message, CancellationToken cancellationToken = default)
	{
		var scope = sp.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AstroidDb>();
		Logger.LogInformation($"Executing order {message.OrderId}.");

		var order = await GetOrder(db, message.OrderId, cancellationToken);
		if (order == null)
		{
			Logger.LogError($"Order not found for {message.OrderId}.");
			return;
		}

		var position = await db.Positions.FirstOrDefaultAsync(x => x.Id == order.PositionId, cancellationToken);
		if (order.Status == OrderStatus.Cancelled)
		{
			order.Reject();
			await AddAudit(db, order, AuditType.OrderRequest, "Order is cancelled.", position?.Id.ToString());
			await db.SaveChangesAsync(cancellationToken);
			Logger.LogError($"Order is cancelled {message.OrderId}.");

			return;
		}

		var isClosing = !order.ClosePosition && position != null && await db.IsPositionClosing(position.Id);
		if (isClosing)
		{
			order.Reject();
			await AddAudit(db, order, AuditType.OrderRequest, "Order is cancelled because position is about to be closed.", position?.Id.ToString());
			await db.SaveChangesAsync(cancellationToken);
			Logger.LogError($"Order is cancelled because position is about to be closed {message.OrderId}.");

			return;
		}

		var request = AMOrderRequest.GenerateRequest(order);
		try
		{
			var exchanger = ExchangerFactory.Create(ServiceProvider, order.Exchange);
			if (exchanger == null)
			{
				order.Reject();
				await AddAudit(db, order, AuditType.OrderRequest, $"Exchanger type {order.Exchange.Label} not found", position?.Id.ToString());
				await db.SaveChangesAsync(cancellationToken);

				return;
			}

			var bot = await db.Bots
				.Where(x => x.UserId == order.UserId)
				.AsNoTracking()
				.FirstOrDefaultAsync(x => x.Id == order.BotId, cancellationToken);

			if (bot == null)
			{
				order.Reject();
				await AddAudit(db, order, AuditType.OrderRequest, $"Bot {order.BotId} not found", position?.Id.ToString());
				await db.SaveChangesAsync(cancellationToken);

				return;
			}

			var result = await exchanger.ExecuteOrder(bot, request);
			result.Audits.ForEach(x =>
			{
				x.UserId = order.UserId;
				x.ActorId = order.BotId;
				x.TargetId = order.Id;
				x.CorrelationId = result.CorrelationId;
				db.Audits.Add(x);
			});

			if (!result.Success)
			{
				Logger.LogError($"Order execution failed for {order.Id}.");
				order.Reject();
				if (position!.Status == PositionStatus.Requested) position.Reject();
			}

			var notification = await db.AddOrderNotification(result.Order, bot, result.Message);
			await db.SaveChangesAsync(cancellationToken);

#pragma warning disable 4014
			if (notification != null) AQNotification.Publish(Mq, new AQONotificationMessage { OrderId = notification.Id }, string.Empty, cancellationToken);
#pragma warning restore 4014
		}
		catch (Exception ex)
		{
			await AddAudit(db, order, AuditType.UnhandledException, ex.Message, position?.Id.ToString());
			order.Reject();
			await db.SaveChangesAsync(cancellationToken);
			Logger.LogError($"[{message.OrderId}] Error {ex.Message}.");
		}
	}

	public async Task<ADOrder?> GetOrder(AstroidDb db, Guid orderId, CancellationToken cancellationToken = default) =>
		await db.Orders
			.Include(x => x.Bot)
			.Include(x => x.Exchange)
				.ThenInclude(x => x.Provider)
			.FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);

	public async Task AddAudit(AstroidDb db, ADOrder order, AuditType type, string description, string? corellationId = null) => await db.AddAudit(order.UserId, order.BotId, type, description, order.Id, corellationId);

	public Guid GetManagerId(IConfiguration config)
	{
		var settings = config.Get<BotManagerConfig>() ?? new();
		var id = Environment.GetEnvironmentVariable("ASTROID_BOT_MANAGER_ID");
		return Guid.TryParse(id, out var guid) ? guid : settings.Id;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		BotManager.Dispose();
		return Task.CompletedTask;
	}
}
