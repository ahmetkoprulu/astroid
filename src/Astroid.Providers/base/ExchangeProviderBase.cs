using Astroid.Core;
using Astroid.Entity;
using Astroid.Providers.Extentions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Astroid.Providers;

public abstract class ExchangeProviderBase : IDisposable
{
	protected ExchangeInfoStore ExchangeStore { get; set; }
	protected ExchangeCalculator Calculator { get; set; }
	protected MetadataMapper MetadataMapper { get; set; }
	protected ExecutionRepository Repo { get; set; }

	protected ExchangeProviderBase(ExecutionRepository repo, ExchangeInfoStore infoStore, ExchangeCalculator calculator, MetadataMapper metadataMapper)
	{
		Repo = repo;
		ExchangeStore = infoStore;
		Calculator = calculator;
		MetadataMapper = metadataMapper;
	}

	public virtual void Context(string properties)
	{
		var propertyValues = JsonConvert.DeserializeObject<List<ProviderPropertyValue>>(properties) ?? throw new ArgumentException("There is no property to bind this provider.");
		MetadataMapper.MapProperties(this, propertyValues);
	}

	public async Task<AMProviderResult> ExecuteOrder(ADBot bot, ADOrder order)
	{
		var result = AMProviderResult.Create(order);
		try
		{
			if (order.TriggerType == OrderTriggerType.Unknown) throw new InvalidOperationException("Order trigger type is unknown.");
			else if (order.TriggerType == OrderTriggerType.Pyramiding) await BuyPyramiding(bot, order, result);
			else if (order.TriggerType == OrderTriggerType.Buy) await Buy(bot, order, result);
			else await Sell(bot, order, result);
		}
		catch (Exception ex)
		{
			result.AddAudit(AuditType.UnhandledException, ex.Message, data: JsonConvert.SerializeObject(new { order.Symbol, order.TriggerType, order.Position.Type })).WithMessage(ex.Message);
			await Repo.RejectOrderWithPosition(order);
		}

		await Repo.SaveChangesAsync();
		return result;
	}

	protected abstract Task<AMProviderResult> Buy(ADBot bot, ADOrder order, AMProviderResult result);
	protected abstract Task<AMProviderResult> BuyPyramiding(ADBot bot, ADOrder order, AMProviderResult result);
	protected abstract Task<AMProviderResult> Sell(ADBot bot, ADOrder order, AMProviderResult result);
	protected abstract Task<AMOrderResult> PlaceMarketOrder(string ticker, decimal quantity, OrderType oType, PositionType pType, bool reduceOnly = false);
	protected abstract Task<AMOrderResult> PlaceOboOrder(AMOrderBook orderBook, string ticker, decimal quantity, OrderType oType, PositionType pType, LimitSettings settings);
	protected abstract Task<AMOrderResult> PlaceDeviatedOrder(string ticker, decimal quantity, decimal price, OrderType oType, PositionType pType);
	protected abstract Task<AMOrderResult> PlaceOrderTillPositionFilled(AMOrderBook orderBook, string ticker, decimal quantity, OrderType oType, PositionType pType, LimitSettings settings);
	protected abstract Task<AMOrderBook> GetOrderBook(AMOrderBook orderBook, string ticker);
	protected abstract Task<decimal> GetBalance(string asset);
	public abstract Task<AMExchangeWallet> GetAccountInfo(Guid id, string name);

	protected async Task<decimal> ConvertUsdtToCoin(ADOrder order, decimal lastPrice, int quantityPrecision, decimal stepSize = 0)
	{
		if (order.QuantityType == PositionSizeType.FixedInAsset) return Math.Round(order.Quantity, quantityPrecision);

		if (order.QuantityType == PositionSizeType.FixedInUsd) return Math.Round(order.Quantity / lastPrice, quantityPrecision);

		var usdtBalance = await GetBalance("USDT");
		var usdQuantity = usdtBalance * Convert.ToDecimal(order.Quantity) / 100;

		return Math.Round(usdQuantity / lastPrice, quantityPrecision);
	}

	protected async Task<AMOrderBook> TryGetOrderBook(string exchange, string ticker)
	{
		var orderBook = await ExchangeStore.GetOrderBook(exchange, ticker);
		if (await orderBook.ReadLastUpdateTime() > 0) return orderBook;

		return await GetOrderBook(orderBook, ticker);
	}

	protected async Task<AMSymbolInfo> GetSymbolInfo(string exchange, string ticker)
	{
		var symbolInfo = await ExchangeStore.GetSymbolInfoLazy(exchange, ticker) ?? throw new Exception($"Could not find symbol info for {ticker}");
		return symbolInfo;
	}

	protected async Task CreateStopLossOrder(ADBot bot, ADOrder order, decimal entryPrice, int pricePrecision)
	{
		if (!bot.IsStopLossEnabled) return;

		var slOrder = await Repo.GetOpenStopLossOrder(order.Position.Id);
		if (slOrder != null) return;

		if (bot.StopLossSettings.Price <= 0) bot.StopLossSettings.Price = 1;

		var stopPrice = Calculator.CalculateStopLoss(bot.StopLossSettings.Price, entryPrice, pricePrecision, order.Position.Type);
		await Repo.AddOrder(order.Position, OrderTriggerType.StopLoss, OrderConditionType.Decreasing, stopPrice, 100, PositionSizeType.Ratio, true);
	}

	protected async Task CreatePyramidingOrders(ADBot bot, ADOrder order, int pricePrecision)
	{
		if (!bot.IsPyramidingEnabled) return;

		await CancelOrders(order.Position, OrderTriggerType.Pyramiding);
		var targets = bot.PyramidingSettings.Targets;
		for (var i = 0; i < targets.Count; i++)
		{
			var target = targets[i];
			// var qty = Math.Round(quantity * target.Quantity / 100, symbol.QuantityPrecision); qty type is same with position size so directly pass to order
			var stopPrice = Calculator.CalculatePyramid(target.Target, order.FilledPrice, pricePrecision, order.Position.Type);
			var condition = target.Target < 0 ? OrderConditionType.Decreasing : OrderConditionType.Increasing;
			await Repo.AddOrder(order.Position, OrderTriggerType.Pyramiding, condition, stopPrice, target.Quantity, bot.PositionSizeType, false);
		}
	}

	protected async Task CreateTakeProfitOrders(ADBot bot, ADOrder order, int pricePrecision, int quantityPrecision)
	{
		if (!bot.IsTakePofitEnabled) return;

		await CancelOrders(order.Position, OrderTriggerType.TakeProfit);
		var price = bot.TakeProfitSettings.CalculationBase == CalculationBase.EntryPrice
			? order.Position.EntryPrice
			: bot.TakeProfitSettings.CalculationBase == CalculationBase.AveragePrice
				? order.Position.AvgEntryPrice
				: order.FilledPrice;

		var prevOrderId = order.Id;
		var targets = bot.TakeProfitSettings.Targets;
		for (var i = 0; i < targets.Count; i++)
		{
			var target = targets[i];
			var qty = Math.Round(order.Position.CurrentQuantity * target.Quantity / 100, quantityPrecision);
			var stopPrice = Calculator.CalculateTakeProfit(target.Target, price, pricePrecision, order.Position.Type);
			var newOrder = await Repo.AddOrder(order.Position, OrderTriggerType.TakeProfit, OrderConditionType.Increasing, stopPrice, qty, PositionSizeType.FixedInAsset, i == targets.Count - 1, prevOrderId);
			prevOrderId = newOrder.Id;
		}
	}

	// TODO: Move to repo
	protected async Task CancelOrders(ADPosition position, OrderTriggerType type)
	{
		var orders = await Repo.GetOpenOrders(position.ExchangeId, position.Id, type);
		if (orders.Count == 0) return;

		foreach (var o in orders)
		{
			o.Status = OrderStatus.Cancelled;
			o.UpdatedDate = DateTime.UtcNow;
		}
	}

	protected async Task<AMOrderResult> PlaceOrder(ADBot bot, ADOrder order, decimal quantity, OrderType oType, PositionType pType, AMSymbolInfo sInfo)
	{
		if (bot.OrderType == OrderEntryType.Limit) return await PlaceLimitOrder(order, quantity, oType, pType, bot.LimitSettings, sInfo);
		return await PlaceMarketOrder(order.Symbol, quantity, oType, pType);
	}

	protected async Task<AMOrderResult> PlaceLimitOrder(ADOrder order, decimal quantity, OrderType oType, PositionType pType, LimitSettings settings, AMSymbolInfo sInfo)
	{
		if (settings.ValorizationType == ValorizationType.LastPrice)
		{
			var price = Calculator.CalculateDeviatedPrice(sInfo.LastPrice, sInfo.PricePrecision, settings.Deviation, pType);
			return await PlaceDeviatedOrder(order.Symbol, quantity, price, oType, pType);
		}

		var orderBook = await TryGetOrderBook(order.Exchange.Provider.Name, order.Symbol);
		if (settings.ForceUntilFilled)
			return await PlaceOrderTillPositionFilled(orderBook, order.Symbol, quantity, oType, pType, settings);

		return await PlaceOboOrder(orderBook, order.Symbol, quantity, oType, pType, settings);
	}

	protected decimal GetPnl(ADPosition position, decimal price, decimal quantity)
	{
		var wAveragePrice = position.WeightedEntryPrice / position.Quantity;
		var pnl = Calculator.CalculatePnl(wAveragePrice, price, quantity, position.Leverage, position.Type);

		return pnl;
	}

	public virtual void Dispose()
	{
		// Db.Dispose();
	}
}
