
using Astroid.Core.Cache;
using Astroid.Core;
using Astroid.Entity;
using Astroid.Providers;
using Microsoft.EntityFrameworkCore;
using Astroid.Entity.Extentions;
using Astroid.Core.MessageQueue;
using Astroid.Core.Helpers;

namespace Astroid.BackgroundServices.Order;

public class Worker : IHostedService
{
	public Guid Id { get; } = Guid.Empty;
	private IServiceProvider ServiceProvider { get; set; }
	private ICacheService Cache { get; set; }
	private ILogger<Worker> Logger { get; set; }
	// private AstroidDb Db { get; set; }
	private BotRegistrationManager BotManager { get; set; }

	public Worker(IConfiguration config, IServiceProvider serviceProvider, BotRegistrationManager botManager, ICacheService cache, ILogger<Worker> logger)
	{
		Id = GetManagerId(config);
		if (Id == Guid.Empty) throw new Exception("Invalid Bot Manager Id.");

		ServiceProvider = serviceProvider;
		BotManager = botManager;
		Cache = cache;
		Logger = logger;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		await BotManager.SubscribeToRegistrations(async (msg, ct) => await ExecuteOrder(ServiceProvider, msg, ct), cancellationToken);
		await BotManager.SubscribeToUnregistrations(cancellationToken);

		Logger.LogInformation("Starting Order Executor Service.");
		_ = Task.Run(() => Ping(cancellationToken), cancellationToken);
		_ = Task.Run(() => RestoreBotConnections(cancellationToken), cancellationToken);
	}

	public Task RestoreBotConnections(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			var addTask = AddBots(cancellationToken);
			var removeTask = RemoveBots(cancellationToken);

			Task.WaitAll(new Task[] { addTask, removeTask }, cancellationToken);

			Task.Delay(1000 * 30, cancellationToken);
		}

		return Task.CompletedTask;
	}

	public async Task AddBots(CancellationToken cancellationToken = default)
	{
		var scope = ServiceProvider.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AstroidDb>();

		var bots = await db.Bots
			.AsNoTracking()
			.Where(x => x.IsEnabled && x.ManagedBy == Id)
			.ToListAsync(cancellationToken);

		foreach (var bot in bots)
		{
			if (BotManager.TryGetOrderQueue(bot.Id) != null) continue;

			await BotManager.Register(new AQBotRegistrationMessage { BotId = bot.Id }, async (msg, ct) => await ExecuteOrder(ServiceProvider, msg, ct), cancellationToken);
		}
	}

	public async Task RemoveBots(CancellationToken cancellationToken = default)
	{
		var scope = ServiceProvider.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AstroidDb>();

		var registrations = BotManager.GetRegistrations();

		foreach (var reg in registrations)
		{
			var bot = await db.Bots
				.AsNoTracking()
				.FirstOrDefaultAsync(x => x.Id == reg.Id, cancellationToken);

			if (bot != null && bot.ManagedBy == Id) continue;

			await BotManager.UnRegister(new AQBotRegistrationMessage { BotId = reg.Id }, cancellationToken);
		}
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

		var manager = await db.BotManagers
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == Id, cancellationToken);

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

		if (order.Status == OrderStatus.Cancelled)
		{
			order.Reject();
			await AddAudit(db, order, AuditType.OrderRequest, "Order is cancelled.", order.Position.Id.ToString());
			await db.SaveChangesAsync(cancellationToken);
			Logger.LogError($"Order is cancelled {message.OrderId}.");

			return;
		}

		var isClosing = !order.ClosePosition && await db.IsPositionClosing(order.Position.Id);
		if (isClosing)
		{
			order.Reject();
			await AddAudit(db, order, AuditType.OrderRequest, "Order is cancelled because position is about to be closed.", order.Position.Id.ToString());
			await db.SaveChangesAsync(cancellationToken);
			Logger.LogError($"Order is cancelled because position is about to be closed {message.OrderId}.");

			return;
		}

		if (await Cache.IsLocked($"lock:bot:{order.BotId}:{order.Symbol}"))
		{
			order.Reject();
			await AddAudit(db, order, AuditType.OrderRequest, "The position has been already managing by another process.", order.Position.Id.ToString());
			await db.SaveChangesAsync(cancellationToken);
			Logger.LogInformation($"The position has been already managing by another process {message.OrderId}.");

			return;
		}

		var exchanger = ExchangerFactory.Create(ServiceProvider, order.Exchange);
		if (exchanger == null)
		{
			order.Reject();
			await AddAudit(db, order, AuditType.OrderRequest, $"Exchanger type {order.Exchange.Label} not found", order.Position.Id.ToString());
			await db.SaveChangesAsync(cancellationToken);

			return;
		}

		var request = AMOrderRequest.GenerateRequest(order);
		var needToLock = order.ClosePosition;

		try
		{
			if (needToLock) await Cache.AcquireLock($"lock:bot:{order.BotId}:{order.Symbol}", TimeSpan.FromMinutes(5));

			var bot = await db.Bots
				.Where(x => x.UserId == order.UserId)
				.AsNoTracking()
				.FirstOrDefaultAsync(x => x.Id == order.BotId, cancellationToken);

			if (bot == null)
			{
				order.Reject();
				await AddAudit(db, order, AuditType.OrderRequest, $"Bot {order.BotId} not found", order.Position.Id.ToString());
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
			}

			await db.SaveChangesAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			await AddAudit(db, order, AuditType.UnhandledException, ex.Message, order.Position.Id.ToString());
			order.Reject();
			await db.SaveChangesAsync(cancellationToken);
			Logger.LogError($"[{message.OrderId}] Error {ex.Message}.");
		}
		finally
		{
			if (needToLock) await Cache.ReleaseLock($"lock:bot:{order.BotId}:{order.Symbol}");
		}
	}

	public async Task<ADOrder?> GetOrder(AstroidDb db, Guid orderId, CancellationToken cancellationToken = default) =>
		await db.Orders
			.Include(x => x.Position)
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
