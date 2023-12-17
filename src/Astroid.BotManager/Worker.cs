
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
		// await BotManager.Subscribe(async (msg, ct) => await ExecuteOrder(ServiceProvider, msg, ct), cancellationToken);
		Logger.LogInformation("Subscribed registration queues.");

		_ = Task.Run(() => Ping(cancellationToken), cancellationToken);
	}

	public async Task ExecuteOrder(IServiceProvider sp, AQOrderMessage message, CancellationToken cancellationToken = default)
	{
		using var scope = sp.CreateScope();
		using var db = scope.ServiceProvider.GetRequiredService<AstroidDb>();
		Logger.LogInformation($"Executing order {message.OrderId}.");

		var order = await GetOrder(db, message.OrderId, cancellationToken);
		if (order == null)
		{
			Logger.LogError($"Order not found for {message.OrderId}.");
			return;
		}

		try
		{
			var bot = await GetBot(db, order.BotId, cancellationToken);
			var exchanger = ExchangerFactory.Create(scope.ServiceProvider, order.Exchange);
			(var success, var msg) = await ValidateTriggeredOrder(order, bot, exchanger, db);
			if (!success)
			{
				order.Reject();
				await AddAudit(db, order, AuditType.OrderRequest, msg, order.Position.Id.ToString());
				await db.SaveChangesAsync(cancellationToken);
				Logger.LogError(msg);

				return;
			}

			var result = await exchanger!.ExecuteOrder(bot!, order);
			result.SaveAudits(db);
			var notification = await db.AddOrderNotification(result.Order, bot!, result.Message);
			await db.SaveChangesAsync(cancellationToken);

			if (notification != null) await AQNotification.Publish(Mq, new AQONotificationMessage { OrderId = notification.Id }, string.Empty, cancellationToken);
		}
		catch (Exception ex)
		{
			order.Reject();
			await AddAudit(db, order, AuditType.UnhandledException, ex.Message, order.PositionId.ToString());
			await db.SaveChangesAsync(cancellationToken);
			Logger.LogError($"[{message.OrderId}] Error {ex.Message}.");
		}
	}

	public async Task<ADOrder?> GetOrder(AstroidDb db, Guid orderId, CancellationToken cancellationToken = default) =>
		await db.Orders
			.Include(x => x.Position)
			.Include(x => x.Bot)
			.Include(x => x.Exchange)
				.ThenInclude(x => x.Provider)
			.FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);

	public async Task<ADBot?> GetBot(AstroidDb db, Guid botId, CancellationToken cancellationToken = default) =>
		await db.Bots.AsNoTracking().FirstOrDefaultAsync(x => x.Id == botId, cancellationToken);

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

	public async Task AddAudit(AstroidDb db, ADOrder order, AuditType type, string description, string? corellationId = null) => await db.AddAudit(order.UserId, order.BotId, type, description, order.Id, corellationId);

	public Guid GetManagerId(IConfiguration config)
	{
		var settings = config.Get<BotManagerConfig>() ?? new();
		var id = Environment.GetEnvironmentVariable("ASTROID_BOT_MANAGER_ID");
		return Guid.TryParse(id, out var guid) ? guid : settings.Id;
	}

	public async Task<(bool, string)> ValidateTriggeredOrder(ADOrder order, ADBot? bot, ExchangeProviderBase? provider, AstroidDb db)
	{
		if (order.Status == OrderStatus.Cancelled) return (false, $"Order {order.Id} is cancelled.");

		var isClosing = !order.ClosePosition && await db.IsPositionClosing(order.PositionId);
		if (isClosing) return (false, $"Order is cancelled because position is about to be closed {order.Id}.");

		if (bot == null) return (false, $"Bot {order.BotId} not found.");

		if (provider == null) return (false, $"Provider {bot.Exchange.Label} not found.");

		return (true, "Success");
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		BotManager.Dispose();
		return Task.CompletedTask;
	}
}
