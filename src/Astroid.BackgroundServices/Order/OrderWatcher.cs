
using Astroid.Core.Cache;
using Astroid.Core;
using Astroid.Entity;
using Astroid.Providers;
using Microsoft.EntityFrameworkCore;
using Astroid.Entity.Extentions;
using Astroid.Core.MessageQueue;

namespace Astroid.BackgroundServices.Order;

public class OrderWatcher : IHostedService
{
	private ExchangeInfoStore ExchangeStore { get; set; }
	private ICacheService Cache { get; set; }
	private ILogger<OrderWatcher> Logger { get; set; }
	private IServiceProvider Services { get; set; }
	// private AstroidDb Db { get; set; }
	private IMessageQueue Mq { get; set; }

	public OrderWatcher(ExchangeInfoStore exchangeStore, IMessageQueue mq, ICacheService cache, ILogger<OrderWatcher> logger, IServiceProvider services)
	{
		ExchangeStore = exchangeStore;
		Mq = mq;
		Cache = cache;
		Logger = logger;
		Services = services;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		Logger.LogInformation("Setting Up Orders Message Queue.");
		Logger.LogInformation("Starting Take Profit Watcher Service.");
		_ = Task.Run(() => DoJob(cancellationToken), cancellationToken);

		return Task.CompletedTask;
	}

	public async Task DoJob(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			var buyTask = WatchBuyOrders(cancellationToken);
			var sellTask = WatchSellOrders(cancellationToken);
			var lossTask = WatchStopLossOrders(cancellationToken);
			var profitTask = WatchTakeProfitOrders(cancellationToken);
			var pyramidingTask = WatchPyramidingOrders(cancellationToken);

			Task.WaitAll(new Task[] { buyTask, sellTask, lossTask, profitTask, pyramidingTask }, cancellationToken: cancellationToken);

			await Task.Delay(1000, cancellationToken);
		}
	}

	public async Task WatchBuyOrders(CancellationToken cancellationToken)
	{
		var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AstroidDb>();
		var orders = db.Orders
			.Include(x => x.Exchange)
			.ThenInclude(x => x.Provider)
			.Where(x => x.TriggerType == OrderTriggerType.Buy && x.Status == OrderStatus.Open)
			.ToList();

		foreach (var order in orders)
		{
			var symbolInfo = await ExchangeStore.GetSymbolInfo(order.Exchange.Provider.Name, order.Symbol);
			if (symbolInfo == null)
			{
				await AddAudit(db, order, AuditType.CloseOrderPlaced, $"Symbol info not found for {order.Symbol} on {order.Exchange.Provider.Name}.", order.Position.Id.ToString());
				continue;
			}

			if (!NeedToTriggerOrder(order.Position, order, symbolInfo.LastPrice)) continue;

			var msg = new AQOrderMessage { OrderId = order.Id };
			order.Status = OrderStatus.Triggered;
			order.UpdatedDate = DateTime.UtcNow;
			await db.SaveChangesAsync(cancellationToken);
			await AQOrder.Publish(Mq, msg, order.BotId.ToString(), cancellationToken);
		}
	}

	public async Task WatchSellOrders(CancellationToken cancellationToken)
	{
		var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AstroidDb>();
		var orders = await GetOpenOrders(db, cancellationToken, OrderTriggerType.Sell);

		foreach (var order in orders)
		{
			if (order.Position.Status == PositionStatus.Closed)
			{
				await CancelOrder(db, order, cancellationToken);
				continue;
			}

			var symbolInfo = await ExchangeStore.GetSymbolInfo(order.Exchange.Provider.Name, order.Symbol);
			if (symbolInfo == null)
			{
				await AddAudit(db, order, AuditType.CloseOrderPlaced, $"Symbol info not found for {order.Symbol} on {order.Exchange.Provider.Name}.", order.Position.Id.ToString());
				continue;
			}

			if (!NeedToTriggerOrder(order.Position, order, symbolInfo.LastPrice)) continue;

			var msg = new AQOrderMessage { OrderId = order.Id };
			order.Status = OrderStatus.Triggered;
			order.UpdatedDate = DateTime.UtcNow;
			await db.SaveChangesAsync(cancellationToken);
			await AQOrder.Publish(Mq, msg, order.BotId.ToString(), cancellationToken);
		}
	}

	public async Task WatchStopLossOrders(CancellationToken cancellationToken)
	{
		var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AstroidDb>();
		var orders = await GetOpenOrders(db, cancellationToken, OrderTriggerType.StopLoss);
		// Logger.LogInformation($"Watching {orders.Count} stop loss orders.");

		foreach (var order in orders)
		{
			if (order.Position.Status == PositionStatus.Closed)
			{
				await CancelOrder(db, order, cancellationToken);
				continue;
			}

			var symbolInfo = await ExchangeStore.GetSymbolInfo(order.Exchange.Provider.Name, order.Symbol);
			if (symbolInfo == null)
			{
				await AddAudit(db, order, AuditType.StopLossOrderPlaced, $"Symbol info not found for {order.Symbol} on {order.Exchange.Provider.Name}.", order.Position.Id.ToString());
				continue;
			}

			if (!NeedToTriggerOrder(order.Position, order, symbolInfo.LastPrice)) continue;

			var msg = new AQOrderMessage { OrderId = order.Id };
			order.Status = OrderStatus.Triggered;
			order.UpdatedDate = DateTime.UtcNow;
			await db.SaveChangesAsync(cancellationToken);
			await AQOrder.Publish(Mq, msg, order.BotId.ToString(), cancellationToken);
		}
	}

	public async Task WatchPyramidingOrders(CancellationToken cancellationToken)
	{
		var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AstroidDb>();
		var orders = await GetOpenOrders(db, cancellationToken, OrderTriggerType.Pyramiding);

		foreach (var order in orders)
		{
			if (order.Position.Status == PositionStatus.Closed)
			{
				await CancelOrder(db, order, cancellationToken);
				continue;
			}

			var symbolInfo = await ExchangeStore.GetSymbolInfo(order.Exchange.Provider.Name, order.Symbol);
			if (symbolInfo == null)
			{
				await AddAudit(db, order, AuditType.OpenOrderPlaced, $"Symbol info not found for {order.Symbol} on {order.Exchange.Provider.Name}.", order.Position.Id.ToString());
				continue;
			}

			if (!NeedToTriggerOrder(order.Position, order, symbolInfo.LastPrice)) continue;
			Logger.LogInformation($"Pyramiding order triggered for {order.Id} {order.Symbol} on {order.Exchange.Provider.Name}.");
			var msg = new AQOrderMessage { OrderId = order.Id };
			order.Status = OrderStatus.Triggered;
			order.UpdatedDate = DateTime.UtcNow;

			db.Orders.Update(order);
			await db.SaveChangesAsync(cancellationToken);
			await AQOrder.Publish(Mq, msg, order.BotId.ToString(), cancellationToken);
		}
	}

	public async Task WatchTakeProfitOrders(CancellationToken cancellationToken)
	{
		var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AstroidDb>();
		var openOrders = await GetOpenOrders(db, cancellationToken, OrderTriggerType.TakeProfit);
		var orders = openOrders.Where(x => x.TriggerType == OrderTriggerType.TakeProfit).ToList();

		foreach (var order in orders)
		{
			if (order.Position.Status == PositionStatus.Closed)
			{
				await CancelOrder(db, order, cancellationToken);
				continue;
			}

			var symbolInfo = await ExchangeStore.GetSymbolInfo(order.Exchange.Provider.Name, order.Symbol);
			if (symbolInfo == null)
			{
				await AddAudit(db, order, AuditType.TakeProfitOrderPlaced, $"Symbol info not found for {order.Symbol} on {order.Exchange.Provider.Name}.", order.Position.Id.ToString());
				continue;
			}

			if (!NeedToTriggerOrder(order.Position, order, symbolInfo.LastPrice)) continue;

			var msg = new AQOrderMessage { OrderId = order.Id };
			order.Status = OrderStatus.Triggered;
			order.UpdatedDate = DateTime.UtcNow;
			await db.SaveChangesAsync(cancellationToken);
			await AQOrder.Publish(Mq, msg, order.BotId.ToString(), cancellationToken);
		}
	}

	public bool NeedToTriggerOrder(ADPosition position, ADOrder order, decimal symbolPrice)
	{
		if (order.ConditionType == OrderConditionType.Immediate)
			return true;

		if (order.ConditionType == OrderConditionType.Decreasing)
			return position.Type == PositionType.Long ? symbolPrice < order.TriggerPrice : symbolPrice > order.TriggerPrice;

		if (order.ConditionType == OrderConditionType.Increasing)
			return position.Type == PositionType.Long ? symbolPrice > order.TriggerPrice : symbolPrice < order.TriggerPrice;

		return false;
	}

	public async Task CancelOrder(AstroidDb db, ADOrder order, CancellationToken cancellationToken)
	{
		order.Status = OrderStatus.Cancelled;
		await db.SaveChangesAsync(cancellationToken);
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

	public async Task<List<ADOrder>> GetOpenOrders(AstroidDb db, CancellationToken cancellationToken, OrderTriggerType triggerType = OrderTriggerType.Unknown)
	{
		var orders = db.Orders
			.Include(x => x.Position)
			.Include(x => x.Exchange)
				.ThenInclude(x => x.Provider)
			.Where(x => x.Status == OrderStatus.Open);

		if (triggerType != OrderTriggerType.Unknown)
			orders = orders.Where(x => x.TriggerType == triggerType);

		return await orders.ToListAsync(cancellationToken);
	}

	public async Task AddAudit(AstroidDb db, ADOrder order, AuditType type, string description, string? corellationId = null) => await db.AddAudit(order.UserId, order.BotId, type, description, order.Id, corellationId);

	public Task StopAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}
