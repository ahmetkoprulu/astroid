
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
		_ = Task.Run(() => DoJob(cancellationToken), cancellationToken);

		return Task.CompletedTask;
	}

	public async Task DoJob(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			Logger.LogInformation("Started Next Order Iteration.");
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

		if (orders.Count != 0) Logger.LogInformation($"\t[BUY] Watching {orders.Count} orders.");

		foreach (var order in orders)
		{
			var symbolInfo = await ExchangeStore.GetSymbolInfo(order.Exchange.Provider.Name, order.Symbol);
			if (symbolInfo == null)
			{
				await AddAudit(db, order, AuditType.CloseOrderPlaced, $"Symbol info not found for {order.Symbol} on {order.Exchange.Provider.Name}.", order.Position.Id.ToString());
				continue;
			}

			var lastPrice = await symbolInfo.GetLastPrice();
			if (!NeedToTriggerOrder(order.Position, order, lastPrice)) continue;

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
		if (orders.Count != 0) Logger.LogInformation($"\t[SELL] Watching {orders.Count} orders.");

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

			var lastPrice = await symbolInfo.GetLastPrice();
			if (!NeedToTriggerOrder(order.Position, order, lastPrice)) continue;

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
		if (orders.Count != 0) Logger.LogInformation($"\t[SL] Watching {orders.Count} orders.");

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

			var lastPrice = await symbolInfo.GetLastPrice(); var pricePricision = await symbolInfo.GetPricePrecision();
			if (!NeedToTriggerOrder(order.Position, order, lastPrice))
			{
				var needToUpdate = UpdateTrailingStop(order, lastPrice, pricePricision);
				if (needToUpdate) await db.SaveChangesAsync(cancellationToken);

				continue;
			}

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
		if (orders.Count != 0) Logger.LogInformation($"\t[PYR] Watching {orders.Count} orders.");

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

			var lastPrice = await symbolInfo.GetLastPrice();
			if (!NeedToTriggerOrder(order.Position, order, lastPrice)) continue;

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
		if (orders.Count != 0) Logger.LogInformation($"\t[TP] Watching {orders.Count} orders.");

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

			var lastPrice = await symbolInfo.GetLastPrice();
			if (!NeedToTriggerOrder(order.Position, order, lastPrice)) continue;

			var msg = new AQOrderMessage { OrderId = order.Id };
			order.Status = OrderStatus.Triggered;
			order.UpdatedDate = DateTime.UtcNow;
			await UpdateTrailingProfit(db, order, cancellationToken);
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

	public bool UpdateTrailingStop(ADOrder order, decimal price, int precision)
	{
		if (order.Bot.StopLossSettings.Type != StopLossType.Trailing) return false;
		if (order.Bot.StopLossSettings.Margin == null || order.Bot.StopLossSettings.Margin <= 0) return false;

		var activationPrice = BinanceUsdFuturesProvider.CalculateTakeProfit(order.Bot.StopLossSettings.Margin.Value, order.Position.EntryPrice, precision, order.Position.Type);
		var isActivated = order.Position.Type == PositionType.Long ? price > activationPrice : price < activationPrice;
		if (!isActivated) return false;

		var nextPrice = BinanceUsdFuturesProvider.GetStopLoss(order.Bot, price, precision, order.Position.Type);
		if (nextPrice == null) return false;

		nextPrice = order.Position.Type == PositionType.Long ? Math.Max(nextPrice.Value, order.TriggerPrice) : Math.Min(nextPrice.Value, order.TriggerPrice);
		if (nextPrice == order.TriggerPrice) return false;

		order.TriggerPrice = nextPrice.Value;

		return true;
	}

	public async Task<bool> UpdateTrailingProfit(AstroidDb db, ADOrder order, CancellationToken cancellationToken)
	{
		if (order.Bot.StopLossSettings.Type != StopLossType.TrailingProfit) return false;

		var stopOrder = await db.Orders.FirstOrDefaultAsync(x => x.PositionId == order.PositionId && x.TriggerType == OrderTriggerType.StopLoss && x.Status == OrderStatus.Open, cancellationToken);
		if (stopOrder == null) return false;

		var price = await GetPreviousTakeProfitOrEntry(db, order, cancellationToken);
		if (price == null) return false;

		stopOrder.TriggerPrice = price.Value;

		return true;
	}

	public async Task<decimal?> GetPreviousTakeProfitOrEntry(AstroidDb db, ADOrder order, CancellationToken cancellationToken)
	{
		if (order.RelatedTo == null || order.RelatedTo == Guid.Empty) return null;

		var previousOrder = await db.Orders.FirstOrDefaultAsync(x => x.Id == order.RelatedTo, cancellationToken);
		if (previousOrder == null) return null;

		if (previousOrder.TriggerType == OrderTriggerType.TakeProfit) return previousOrder.TriggerPrice;

		if (order.Bot.TakeProfitSettings.CalculationBase == CalculationBase.EntryPrice) return order.Position.EntryPrice;

		if (order.Bot.TakeProfitSettings.CalculationBase == CalculationBase.AveragePrice) return order.Position.AvgEntryPrice;

		// if calculation based on last price, get open or pyramiding order trigger price what so ever.
		return previousOrder.TriggerPrice;
	}

	public async Task CancelOrder(AstroidDb db, ADOrder order, CancellationToken cancellationToken)
	{
		order.Status = OrderStatus.Cancelled;
		await db.SaveChangesAsync(cancellationToken);
	}

	public async Task<List<ADOrder>> GetOpenOrders(AstroidDb db, CancellationToken cancellationToken, OrderTriggerType triggerType = OrderTriggerType.Unknown)
	{
		var orders = db.Orders
			.Include(x => x.Position)
			.Include(x => x.Exchange)
				.ThenInclude(x => x.Provider)
			.Include(x => x.Bot)
			.Where(x => x.Status == OrderStatus.Open);

		if (triggerType != OrderTriggerType.Unknown)
			orders = orders.Where(x => x.TriggerType == triggerType);

		return await orders.ToListAsync(cancellationToken);
	}

	public async Task AddAudit(AstroidDb db, ADOrder order, AuditType type, string description, string? corellationId = null) => await db.AddAudit(order.UserId, order.BotId, type, description, order.Id, corellationId);

	public Task StopAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}
