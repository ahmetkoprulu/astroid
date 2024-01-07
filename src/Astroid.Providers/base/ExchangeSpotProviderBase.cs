using Astroid.Core;
using Astroid.Entity;
using Newtonsoft.Json;

namespace Astroid.Providers;

public abstract class ExchangeSpotProviderBase : ExchangeProviderBase
{
	public ExchangeSpotProviderBase(ExecutionRepository repo, ExchangeInfoStore infoStore, ExchangeCalculator calculator, MetadataMapper metadataMapper) : base(repo, infoStore, calculator, metadataMapper) { }

	protected override async Task<AMProviderResult> Buy(ADBot bot, ADOrder order, AMProviderResult result)
	{
		if (!result.ValidateOpenRequest(bot)) return result;

		var symbolInfo = await GetSymbolInfo(order.Exchange.Provider.Name, order.Symbol);
		var quantity = await ConvertUsdtToCoin(order, symbolInfo.LastPrice, symbolInfo.QuantityPrecision);
		quantity = NormalizeQuantity(quantity, symbolInfo.QuantityPrecision, symbolInfo.StepSize);

		var orderResult = await PlaceOrder(bot, order, quantity, OrderType.Buy, order.Position.Type, symbolInfo);
		if (!orderResult.Success)
		{
			order.Reject();
			return result.WithMessage(orderResult?.Message ?? "");
		}

		Repo.ExpandPosition(order, orderResult.Quantity, orderResult.EntryPrice, 1);
		if (result.ValidateStopLoss(bot)) await CreateStopLossOrder(bot, order, symbolInfo.LastPrice, symbolInfo.PricePrecision);
		if (result.ValidateTakeProfit(bot)) await CreateTakeProfitOrders(bot, order, symbolInfo.PricePrecision, symbolInfo.QuantityPrecision);
		if (result.ValidatePyramiding(bot)) await CreatePyramidingOrders(bot, order, symbolInfo.PricePrecision);

		return result;
	}

	protected override async Task<AMProviderResult> BuyPyramiding(ADBot bot, ADOrder order, AMProviderResult result)
	{
		if (!result.ValidatePyramiding(bot)) return result;

		var symbolInfo = await GetSymbolInfo(order.Exchange.Provider.Name, order.Symbol);
		var quantity = await ConvertUsdtToCoin(order, symbolInfo.LastPrice, symbolInfo.QuantityPrecision);
		quantity = NormalizeQuantity(quantity, symbolInfo.QuantityPrecision, symbolInfo.StepSize);

		var orderResult = await PlaceOrder(bot, order, quantity, OrderType.Buy, order.Position.Type, symbolInfo);
		if (!orderResult.Success)
		{
			order.Reject();
			return result.AddAudit(AuditType.OpenOrderPlaced, $"Failed to open pyramiding position: {orderResult.Message}", data: JsonConvert.SerializeObject(new { order.Symbol, OrderType = "Buy", PositionType = order.Position.Type })).WithMessage(orderResult?.Message ?? "");
		}

		Repo.ExpandPosition(order, orderResult.Quantity, orderResult.EntryPrice);
		if (bot.IsStopLossEnabled) await CreateStopLossOrder(bot, order, symbolInfo.LastPrice, symbolInfo.PricePrecision);
		if (bot.IsTakePofitEnabled) await CreateTakeProfitOrders(bot, order, symbolInfo.QuantityPrecision, symbolInfo.PricePrecision);

		return result;
	}

	protected override async Task<AMProviderResult> Sell(ADBot bot, ADOrder order, AMProviderResult result)
	{
		if (!result.ValidateCloseRequest(bot)) return result;

		if (order.Position.Status == PositionStatus.Requested)
		{
			Repo.CloseRequestedPosition(order.Position, order);
			return result.WithSuccess();
		}

		var symbolInfo = await GetSymbolInfo(order.Exchange.Provider.Name, order.Symbol);
		var quantity = Calculator.CalculateAssetQuantity(order.Quantity, order.QuantityType, symbolInfo.LastPrice, symbolInfo.QuantityPrecision, order.Position.CurrentQuantity);
		quantity = NormalizeQuantity(quantity, symbolInfo.QuantityPrecision, symbolInfo.StepSize);
		var orderResult = await PlaceMarketOrder(order.Symbol, quantity, OrderType.Sell, order.Position.Type);
		var pnl = GetPnl(order.Position, orderResult.EntryPrice, orderResult.Quantity);

		if (!order.ClosePosition) order.ClosePosition = ShouldClosePosition(order, order.Position, orderResult.Quantity, symbolInfo.StepSize);
		await Repo.ReducePosition(order, orderResult.Success, orderResult.Quantity, pnl, orderResult.NeedToReject);

		if (!orderResult.Success) return result.AddAudit(AuditType.CloseOrderPlaced, $"Failed to sell {order.Symbol} - ${order.Position.Type}: {orderResult.Message}", data: JsonConvert.SerializeObject(new { order.Symbol, OrderType = "Sell", PositionType = order.Position.Type })).WithMessage($"Failed to sell {order.Symbol} - ${order.Position.Type}: {orderResult.Message}");

		return result.WithSuccess();
	}


	protected bool ShouldClosePosition(ADOrder order, ADPosition position, decimal quantity, decimal stepSize)
	{
		if (order.ClosePosition) return true;

		if (quantity > position.CurrentQuantity - stepSize) return true;

		return false;
	}

	protected decimal NormalizeQuantity(decimal quantity, int quantityPrecision, decimal stepSize)
	{
		var reminder = quantityPrecision > 0 ? quantity % stepSize : 0;

		return quantity - reminder;
	}
}
