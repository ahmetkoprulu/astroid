
using Astroid.Core.Cache;
using Astroid.Core;
using Astroid.Entity;
using Astroid.Providers;
using Microsoft.EntityFrameworkCore;
using Astroid.Entity.Extentions;
using Astroid.Core.MessageQueue;

namespace Astroid.BackgroundServices.Cache;

public class OrderWatcher : IHostedService
{
	private ExchangeInfoStore ExchangeStore { get; set; }
	private ICacheService Cache { get; set; }
	private ILogger<OrderWatcher> Logger { get; set; }
	private IServiceProvider Services { get; set; }
	private AstroidDb Db { get; set; }
	private AQOrder OrderQueue { get; set; }

	public OrderWatcher(AstroidDb db, ExchangeInfoStore exchangeStore, AQOrder orderQueue, ICacheService cache, ILogger<OrderWatcher> logger, IServiceProvider services)
	{
		Db = db;
		ExchangeStore = exchangeStore;
		OrderQueue = orderQueue;
		Cache = cache;
		Logger = logger;
		Services = services;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		Logger.LogInformation("Setting Up Orders Message Queue.");
		await OrderQueue.Setup(cancellationToken);
		Logger.LogInformation("Starting Take Profit Watcher Service.");
		_ = Task.Run(() => DoJob(cancellationToken), cancellationToken);
	}

	public async Task DoJob(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			var orders = await GetOpenOrders(cancellationToken);
			var lossTask = WatchStopLossOrders(orders, cancellationToken);
			var profitTask = WatchTakeProfitOrders(orders, cancellationToken);

			Task.WaitAll(new Task[] { lossTask, profitTask }, cancellationToken: cancellationToken);

			await Task.Delay(1000, cancellationToken);
		}
	}

	public async Task WatchStopLossOrders(List<ADOrder> openOrders, CancellationToken cancellationToken)
	{
		var orders = openOrders.Where(x => x.TriggerType == OrderTriggerType.StopLoss).ToList();
		Logger.LogInformation($"Watching {orders.Count} stop loss orders.");
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

	public async Task WatchTakeProfitOrders(List<ADOrder> openOrders, CancellationToken cancellationToken)
	{
		var orders = openOrders.Where(x => x.TriggerType == OrderTriggerType.TakeProfit).ToList();
		Logger.LogInformation($"Watching {orders.Count} take profit orders.");
		foreach (var order in orders)
		{
			var symbolInfo = await ExchangeStore.GetSymbolInfo(order.Exchange.Provider.Name, order.Symbol);
			if (symbolInfo == null)
			{
				await AddAudit(order, AuditType.TakeProfitOrderPlaced, $"Symbol info not found for {order.Symbol} on {order.Exchange.Provider.Name}.", order.Position.Id.ToString());
				continue;
			}

			if (symbolInfo.LastPrice < order.TriggerPrice) continue;

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
			orders = orders.Where(x => x.TriggerType == OrderTriggerType.StopLoss);

		return await orders.ToListAsync(cancellationToken);
	}

	public async Task AddAudit(ADOrder order, AuditType type, string description, string? corellationId = null) => await Db.AddAudit(order.UserId, order.BotId, type, description, order.Id, corellationId);

	public Task StopAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}
