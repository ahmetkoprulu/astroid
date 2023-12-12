
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
	private AQPriceChanges PriceChangesQueue { get; set; }
	private ILogger<OrderWatcher> Logger { get; set; }
	private IServiceProvider Services { get; set; }
	// private AstroidDb Db { get; set; }
	private IMessageQueue Mq { get; set; }

	public OrderWatcher(ExchangeInfoStore exchangeStore, IMessageQueue mq, AQPriceChanges priceChangesQueue, ILogger<OrderWatcher> logger, IServiceProvider services)
	{
		ExchangeStore = exchangeStore;
		Mq = mq;
		PriceChangesQueue = priceChangesQueue;
		Logger = logger;
		Services = services;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		await PriceChangesQueue.Setup(cancellationToken);
		await PriceChangesQueue.Subscribe(PriceChanged, cancellationToken);
		// await PriceChangesQueue.Subscribe(PriceChanged, cancellationToken);

		// _ = Task.Run(() => DoJob(cancellationToken), cancellationToken);
	}

	public async Task PriceChanged(AQPriceChangeMessage msg, CancellationToken cancellationToken)
	{
		var scope = Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AstroidDb>();
		// TODO: Add Exchange Provider Id to the message in order to avoid plus join.
		var openOrders = await GetOpenOrders(db, msg, cancellationToken);
		openOrders
			.ForEach(x =>
			{
				if (NeedToTriggerOrder(x, msg.Prices[x.Symbol]))
				{
					db.Entry(x).Property(x => x.TriggerPrice).IsModified = true;
					x.Status = OrderStatus.Triggered;
					db.Entry(x).Property(x => x.UpdatedDate).IsModified = true;
					x.UpdatedDate = DateTime.UtcNow;

					if (x.TriggerType == OrderTriggerType.TakeProfit) UpdateTrailingProfit(db, openOrders, x);

					return;
				}

				if (x.TriggerType == OrderTriggerType.StopLoss) UpdateTrailingStop(db, x, msg.Prices[x.Symbol]);
			});

		var taskDb = db.SaveChangesAsync(cancellationToken);
		var triggeredOrders = openOrders
			.Where(x => x.Status == OrderStatus.Triggered)
			.ToList();
		await taskDb;

		var tasksMq = triggeredOrders
			.Select(x => AQOrder.Publish(Mq, new AQOrderMessage { OrderId = x.Id }, x.Bot.ManagedBy?.ToString() ?? string.Empty, cancellationToken))
			.ToArray();
		Task.WaitAll(tasksMq, cancellationToken);
	}

	public async Task<List<ADOrder>> GetOpenOrders(AstroidDb db, AQPriceChangeMessage msg, CancellationToken cancellationToken)
	{
		var symbols = msg.Prices.Keys.ToList();
		return await db.Orders
				.AsNoTracking()
				.Include(x => x.Position)
				.Include(x => x.Bot)
				.Include(x => x.Exchange)
				.Where(x => x.Exchange.ProviderId == msg.ExchangeId)
				.Where(x => symbols.Contains(x.Symbol))
				.Where(x => x.Status == OrderStatus.Open)
				.Select(x => new ADOrder
				{
					Id = x.Id,
					PositionId = x.PositionId,
					Symbol = x.Symbol,
					TriggerType = x.TriggerType,
					TriggerPrice = x.TriggerPrice,
					ConditionType = x.ConditionType,
					Bot = new ADBot { IsStopLossEnabled = x.Bot.IsStopLossEnabled, IsTakePofitEnabled = x.Bot.IsTakePofitEnabled, StopLossSettingsJson = x.Bot.StopLossSettingsJson, TakeProfitSettingsJson = x.Bot.TakeProfitSettingsJson },
					Position = new ADPosition { Type = x.Position.Type, EntryPrice = x.Position.EntryPrice, AvgEntryPrice = x.Position.AvgEntryPrice },
					Status = x.Status,
					RelatedTo = x.RelatedTo,
				})
				.ToListAsync(cancellationToken);
	}

	public bool NeedToTriggerOrder(ADOrder order, decimal symbolPrice)
	{
		if (order.ConditionType == OrderConditionType.Immediate)
		{
			order.TriggerPrice = symbolPrice;
			return true;
		}

		if (order.ConditionType == OrderConditionType.Decreasing)
			return order.Position.Type == PositionType.Long ? symbolPrice < order.TriggerPrice : symbolPrice > order.TriggerPrice;

		if (order.ConditionType == OrderConditionType.Increasing)
			return order.Position.Type == PositionType.Long ? symbolPrice > order.TriggerPrice : symbolPrice < order.TriggerPrice;

		return false;
	}

	public bool UpdateTrailingStop(AstroidDb db, ADOrder order, decimal price)
	{
		if (order.Bot.StopLossSettings.Type != StopLossType.Trailing) return false;

		var precision = price.GetPrecisionNumber();
		var activationPrice = ExchangeProviderBase.CalculateTakeProfit(order.Bot.StopLossSettings.Margin ?? 0, order.Position.EntryPrice, precision, order.Position.Type);
		var isActivated = order.Position.Type == PositionType.Long ? price > activationPrice : price < activationPrice;
		if (!isActivated) return false;

		var nextPrice = ExchangeProviderBase.CalculateStopLoss(order.Bot.StopLossSettings.Price, price, precision, order.Position.Type);
		nextPrice = order.Position.Type == PositionType.Long ? Math.Max(nextPrice, order.TriggerPrice) : Math.Min(nextPrice, order.TriggerPrice);
		if (nextPrice == order.TriggerPrice) return false;

		db.Entry(order).Property(x => x.TriggerPrice).IsModified = true;
		order.TriggerPrice = nextPrice;

		db.Entry(order).Property(x => x.UpdatedDate).IsModified = true;
		order.UpdatedDate = DateTime.UtcNow;

		return true;
	}

	public bool UpdateTrailingProfit(AstroidDb db, List<ADOrder> orders, ADOrder order)
	{
		if (order.Bot.StopLossSettings.Type != StopLossType.TrailingProfit) return false;

		var stopOrder = orders.FirstOrDefault(x => x.PositionId == order.PositionId && x.TriggerType == OrderTriggerType.StopLoss && x.Status == OrderStatus.Open);
		if (stopOrder == null) return false;

		var price = GetPreviousTakeProfitOrEntry(db, order);
		if (price == null) return false;

		db.Entry(stopOrder).Property(x => x.TriggerPrice).IsModified = true;
		stopOrder.TriggerPrice = price.Value;

		return true;
	}

	public decimal? GetPreviousTakeProfitOrEntry(AstroidDb db, ADOrder order)
	{
		if (order.RelatedTo == null || order.RelatedTo == Guid.Empty) return null;

		var previousOrder = db.Orders.FirstOrDefault(x => x.Id == order.RelatedTo);
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
