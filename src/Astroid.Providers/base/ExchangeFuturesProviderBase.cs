using Astroid.Core;
using Astroid.Entity;
using Astroid.Providers.Extentions;
using Newtonsoft.Json;

namespace Astroid.Providers;

public abstract class ExchangeFuturesProviderBase : ExchangeProviderBase
{
	protected ExchangeFuturesProviderBase(ExecutionRepository repo, ExchangeInfoStore infoStore, ExchangeCalculator calculator, MetadataMapper metadataMapper) : base(repo, infoStore, calculator, metadataMapper) { }

	protected abstract Task<IEnumerable<AMExchangePosition>> GetPositions();
	protected abstract Task ChangeLeverage(string ticker, int leverage);
	protected abstract Task ChangeMarginType(string ticker, MarginType marginType, AMProviderResult result);

	protected override async Task<AMProviderResult> Buy(ADBot bot, ADOrder order, AMProviderResult result)
	{
		if (!result.ValidateOpenRequest(bot)) return result;

		if (bot.OrderMode == OrderMode.Swing && order.Position.Status == PositionStatus.Requested)
		{
			var closeResult = await ClosePosition(order, order.Position.Type);
			if (!closeResult.Success)
			{
				await Repo.RejectOrderWithPosition(order);
				return result.AddAudit(AuditType.OpenOrderPlaced, $"Failed to swing while closing {order.Symbol} - ${order.Position.Type.Reverse()}: {closeResult.Message}", data: JsonConvert.SerializeObject(new { order.Symbol, OrderType = "Buy", PositionType = order.Position.Type })).WithMessage($"Failed to swing while closing {order.Symbol} - ${order.Position.Type.Reverse()}: {closeResult.Message}");
			}
		}

		var symbolInfo = await GetSymbolInfo(order.Exchange.Provider.Name, order.Symbol);
		var quantity = await ConvertUsdtToCoin(order, symbolInfo.LastPrice, symbolInfo.QuantityPrecision);
		var lev = await ChangeLeverage(order, bot);

		var orderResult = await PlaceOrder(bot, order, quantity, OrderType.Buy, order.Position.Type, symbolInfo);
		if (!orderResult.Success)
		{
			order.Reject();
			return result.WithMessage(orderResult?.Message ?? "");
		}

		Repo.ExpandPosition(order, orderResult.Quantity, orderResult.EntryPrice, lev);
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

		if (order.ClosePosition)
		{
			var cpResult = await ClosePosition(order, order.Position.Type, order.Position);
			if (!cpResult.Success) return result.AddAudit(AuditType.CloseOrderPlaced, $"Failed to close {order.Symbol} - ${order.Position.Type}: {cpResult.Message}", data: JsonConvert.SerializeObject(new { order.Symbol, OrderType = "Sell", PositionType = order.Position.Type })).WithMessage($"Failed to close {order.Symbol} - ${order.Position.Type}: {cpResult.Message}");

			return result.WithSuccess();
		}

		var symbolInfo = await GetSymbolInfo(order.Exchange.Provider.Name, order.Symbol);
		var quantity = Calculator.CalculateAssetQuantity(order.Quantity, order.QuantityType, symbolInfo.LastPrice, symbolInfo.QuantityPrecision, order.Position.Quantity);
		var orderResult = await PlaceMarketOrder(order.Symbol, quantity, OrderType.Sell, order.Position.Type);
		var pnl = GetPnl(order.Position, orderResult.EntryPrice, orderResult.Quantity);
		await Repo.ReducePosition(order, orderResult.Success, orderResult.Quantity, pnl, orderResult.NeedToReject);
		if (!orderResult.Success) return result.AddAudit(AuditType.CloseOrderPlaced, $"Failed to sell {order.Symbol} - ${order.Position.Type}: {orderResult.Message}", data: JsonConvert.SerializeObject(new { order.Symbol, OrderType = "Sell", PositionType = order.Position.Type })).WithMessage($"Failed to sell {order.Symbol} - ${order.Position.Type}: {orderResult.Message}");

		return result.WithSuccess();
	}

	protected async Task<AMOrderResult> ClosePosition(ADOrder order, PositionType pType, ADPosition? position = null)
	{
		position ??= await Repo.GetCurrentPosition(order.ExchangeId, order.Symbol, pType);
		var exPosition = await GetPosition(order.Symbol, pType);

		if (exPosition == null)
		{
			await Repo.CancelOpenOrders(position, closePosition: true);
			return AMOrderResult.WithSuccess();
		}

		var quantity = Math.Max(position.CurrentQuantity, Math.Abs(exPosition.Quantity));
		var orderResult = await PlaceMarketOrder(order.Symbol, quantity, OrderType.Sell, pType);
		if (orderResult.Success)
		{
			var pnl = GetPnl(order.Position, orderResult.EntryPrice, orderResult.Quantity);
			await Repo.ClosePosition(order, orderResult.Quantity, orderResult.EntryPrice, pnl);
		}

		return orderResult;
	}

	protected async Task<AMProviderResult> ChangeTickersMarginType(List<string> tickers, MarginType marginType)
	{
		var result = AMProviderResult.Create();
		foreach (var ticker in tickers) await ChangeMarginType(ticker.ToUpper(), marginType, result);

		return result.WithSuccess();
	}

	protected async Task<int> ChangeLeverage(ADOrder order, ADBot bot)
	{
		var lev = order.Leverage ?? bot.Leverage;
		if (lev <= 0) throw new InvalidOperationException("Leverage could not be zero. Consider adding defalt leverage to bot.");

		await ChangeLeverage(order.Symbol, lev);
		return lev;
	}

	protected async Task<AMExchangePosition?> GetPosition(string ticker, PositionType type)
	{
		var positions = await GetPositions();
		var position = positions.FirstOrDefault(x => x.Symbol == ticker && x.Type == type && x.Quantity != 0);

		if (position == null) return null;

		return position;
	}
}
