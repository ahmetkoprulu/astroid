
using Astroid.Core.Cache;
using Astroid.Core;
using Astroid.Entity;
using Astroid.Providers;
using Microsoft.EntityFrameworkCore;
using Astroid.Entity.Extentions;
using Astroid.Core.MessageQueue;

namespace Astroid.BackgroundServices.Cache;

public class StopLossWatcher : IHostedService
{
	private ExchangeInfoStore ExchangeStore { get; set; }
	private ICacheService Cache { get; set; }
	private ILogger<StopLossWatcher> Logger { get; set; }
	private IServiceProvider Services { get; set; }
	private AstroidDb Db { get; set; }
	private AQOrder OrderQueue { get; set; }

	public StopLossWatcher(AstroidDb db, ExchangeInfoStore exchangeStore, AQOrder orderQueue, ICacheService cache, ILogger<StopLossWatcher> logger, IServiceProvider services)
	{
		Db = db;
		ExchangeStore = exchangeStore;
		OrderQueue = orderQueue;
		Cache = cache;
		Logger = logger;
		Services = services;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		Logger.LogInformation("Starting Take Profit Watcher Service.");
		_ = Task.Run(() => DoJob(WatchStopLossOrders, cancellationToken), cancellationToken);

		return Task.CompletedTask;
	}

	public async Task DoJob(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			await action(cancellationToken);
			await Task.Delay(1000, cancellationToken);
		}
	}

	public async Task WatchStopLossOrders(CancellationToken cancellationToken)
	{
		var orders = await GetOpenOrders(cancellationToken, OrderTriggerType.StopLoss);
		foreach (var order in orders)
		{
			var symbolInfo = await ExchangeStore.GetSymbolInfo(order.Exchange.Provider.Name, order.Symbol);
			if (symbolInfo == null)
			{
				await AddAudit(order, AuditType.TakeProfitOrderPlaced, $"Symbol info not found for {order.Symbol} on {order.Exchange.Provider.Name}.", order.Position.Id.ToString());
				continue;
			}

			if (symbolInfo.MarkPrice > order.TriggerPrice) continue;

			var msg = new AQOrderMessage { OrderId = order.Id };
			await OrderQueue.Publish(msg, cancellationToken);
			order.Status = OrderStatus.Triggered;
			order.UpdatedDate = DateTime.UtcNow;
			await Db.SaveChangesAsync(cancellationToken);
		}
	}

	// public async Task WatchPyramidingOrders(CancellationToken cancellationToken)
	// {
	// 	var orders = await GetOpenOrders(cancellationToken, OrderTriggerType.Pyramiding);
	// 	foreach (var order in orders)
	// 	{
	// 		var symbolInfo = await ExchangeStore.GetSymbolInfo(order.Exchange.Provider.Name, order.Symbol);
	// 		if (symbolInfo == null)
	// 		{
	// 			Logger.LogError($"Symbol info not found for {order.Symbol} on {order.Exchange.Provider.Name}.");
	// 			continue;
	// 		}
	// 	}
	// }

	public async Task<List<ADOrder>> GetOpenOrders(CancellationToken cancellationToken, OrderTriggerType triggerType = OrderTriggerType.Unknown)
	{
		var orders = Db.Orders
			.Include(x => x.Position)
			.Include(x => x.Exchange)
				.ThenInclude(x => x.Provider)
			.Where(x => x.Status == OrderStatus.Open);

		if (triggerType != OrderTriggerType.Unknown)
			orders = orders.Where(x => x.TriggerType == triggerType);

		return await orders.ToListAsync(cancellationToken);
	}

	public async Task AddAudit(ADOrder order, AuditType type, string description, string? corellationId = null) => await Db.AddAudit(order.UserId, order.BotId, type, description, order.Id, corellationId);

	public Task StopAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}
