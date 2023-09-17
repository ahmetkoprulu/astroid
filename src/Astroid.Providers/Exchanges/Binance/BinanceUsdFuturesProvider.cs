using Astroid.Core;
using Astroid.Entity;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models;
using Microsoft.EntityFrameworkCore;
using Astroid.Providers.Extentions;
using CryptoExchange.Net.Authentication;
using Binance.Net;

namespace Astroid.Providers;

public class BinanceUsdFuturesProvider : ExchangeProviderBase
{
	[PropertyMetadata("API Key", Type = PropertyTypes.Text, Required = true, Encrypted = true, Group = "General")]
	public string Key { get; set; } = string.Empty;

	[PropertyMetadata("API Secret", Type = PropertyTypes.Text, Required = true, Encrypted = true, Group = "General")]
	public string Secret { get; set; } = string.Empty;

	[PropertyMetadata("Test Net", Type = PropertyTypes.Boolean, Group = "General")]
	public bool IsTestNet { get; set; }

	private BinanceRestClient Client { get; set; }

	public BinanceUsdFuturesProvider(IServiceProvider serviceProvider, ADExchange exchange) : base(serviceProvider, exchange) => Context();

	public override void Context()
	{
		base.Context();

		Client = new BinanceRestClient(o =>
		{
			o.ApiCredentials = new ApiCredentials(Key, Secret);
			if (IsTestNet) o.Environment = BinanceEnvironment.Testnet;
		});
	}

	public override async Task<AMProviderResult> ExecuteOrder(ADBot bot, AMOrderRequest order)
	{
		var result = new AMProviderResult() { CorrelationId = CorrelationId };
		try
		{
			if (order.OrderType == OrderType.Buy && order.PositionType == PositionType.Long)
			{
				_ = await OpenLong(bot, order, result);
			}
			else if (order.OrderType == OrderType.Sell && order.PositionType == PositionType.Long)
			{
				_ = await CloseLong(bot, order, result);
			}
			else if (order.OrderType == OrderType.Sell && order.PositionType == PositionType.Short)
			{
				_ = await OpenShort(bot, order, result);
			}
			else if (order.OrderType == OrderType.Buy && order.PositionType == PositionType.Short)
			{
				_ = await CloseShort(bot, order, result);
			}
			else if (order.OrderType == OrderType.Sell && order.PositionType == PositionType.Both)
			{
				_ = await CloseAll(order, result);
			}
			else throw new InvalidOperationException("Order could not be executed.");
		}
		catch (Exception ex)
		{
			result.WithMessage(ex.Message);
			result.AddAudit(AuditType.UnhandledException, ex.Message, CorrelationId, data: JsonConvert.SerializeObject(new { order.Ticker, order.OrderType, order.PositionType }));
		}

		return result;
	}

	private async Task<AMProviderResult> OpenLong(ADBot bot, AMOrderRequest order, AMProviderResult result)
	{
		var position = await GetCurrentPosition(order.Ticker, PositionType.Long);
		if (!order.ValidateOpenRequest(position, bot, result)) return result;

		if (bot.OrderMode == OrderMode.Swing && position == null)
		{
			var success = await CloseShort(bot, order.GetSwingRequest()!, result);
			if (!success)
			{
				result.AddAudit(AuditType.OpenOrderPlaced, $"Failed to swing while closing {order.Ticker} - Short", CorrelationId, data: JsonConvert.SerializeObject(new { order.Ticker, OrderType = "Buy", PositionType = "Long" }));
				return result;
			}
		}

		var quantity = await ConvertUsdtToCoin(bot, order);
		await Client.UsdFuturesApi.Account.ChangeInitialLeverageAsync(order.Ticker, order.Leverage);

		AMOrderResult orderResult;
		if (bot.OrderType == OrderEntryType.Market) orderResult = await PlaceMarketOrder(order.Ticker, quantity, OrderSide.Buy, PositionSide.Long, result);
		else orderResult = await PlaceLimitOrder(order.Ticker, quantity, OrderSide.Buy, PositionSide.Long, bot.LimitSettings, result);

		if (!orderResult.Success) return result;

		if (position == null) position = await AddPosition(bot, order, orderResult);
		else UpdatePosition(position, orderResult);

		result.CorrelationId = position.Id.ToString();
		var symbolInfo = await GetSymbolInfo(order.Ticker);
		if (bot.IsStopLossEnabled) await CreateStopLossOrder(bot, position, order, symbolInfo.MarkPrice, symbolInfo, position.CurrentQuantity, result);
		if (bot.IsTakePofitEnabled) await CreateTakeProfitOrders(bot, position, order, orderResult.EntryPrice, symbolInfo, position.CurrentQuantity, result);

		return result;
	}

	private async Task<bool> CloseLong(ADBot bot, AMOrderRequest request, AMProviderResult result)
	{
		var position = await GetCurrentPosition(request.Ticker, request.PositionType);
		if (position == null)
		{
			result.WithMessage("No open long position found").AddAudit(AuditType.CloseOrderPlaced, $"No open long position found", CorrelationId, data: JsonConvert.SerializeObject(new { request.Ticker, OrderType = "Sell", PositionType = "Long" }));
			return true;
		}

		var order = await GetOrder(request.OrderId);
		if (!request.ValidateCloseRequest(position, order, bot, result))
			return false;

		result.CorrelationId = position.Id.ToString();
		var quantity = position.Quantity;

		if (request.Quantity > 0)
		{
			var symbolInfo = await GetSymbolInfo(request.Ticker);
			if (request.QuantityType == QuantityType.Exact)
				quantity = request.Quantity.Value;
			else
				quantity = Math.Round(position.Quantity * (request.Quantity.Value / 100), symbolInfo.QuantityPrecision);
		}

		var closePosition = order == null || order.ClosePosition;
		var orderResult = await PlaceMarketOrder(request.Ticker, quantity, OrderSide.Sell, PositionSide.Long, result, closePosition);

		await ReducePosition(position, orderResult, order);
		if (!orderResult.Success) return false;

		result.WithSuccess();
		return true;
	}

	private async Task<AMProviderResult> OpenShort(ADBot bot, AMOrderRequest order, AMProviderResult result)
	{
		var position = await GetCurrentPosition(order.Ticker, PositionType.Short);
		if (!order.ValidateOpenRequest(position, bot, result)) return result;

		if (bot.OrderMode == OrderMode.Swing && position == null)
		{
			var success = await CloseLong(bot, order.GetSwingRequest()!, result);
			if (!success)
			{
				result.AddAudit(AuditType.OpenOrderPlaced, $"Failed to swing while closing {order.Ticker} - Short", CorrelationId, data: JsonConvert.SerializeObject(new { order.Ticker, OrderType = "Buy", PositionType = "Long" }));
				return result;
			}
		}

		var quantity = await ConvertUsdtToCoin(bot, order);
		await Client.UsdFuturesApi.Account.ChangeInitialLeverageAsync(order.Ticker, order.Leverage);

		AMOrderResult orderResult;
		if (bot.OrderType == OrderEntryType.Market) orderResult = await PlaceMarketOrder(order.Ticker, quantity, OrderSide.Sell, PositionSide.Short, result);
		else orderResult = await PlaceLimitOrder(order.Ticker, quantity, OrderSide.Sell, PositionSide.Short, bot.LimitSettings, result);

		if (!orderResult.Success) return result;

		if (position == null) position = await AddPosition(bot, order, orderResult);
		else UpdatePosition(position, orderResult);

		result.CorrelationId = position.Id.ToString();
		var symbolInfo = await GetSymbolInfo(order.Ticker);
		if (bot.IsStopLossEnabled) await CreateStopLossOrder(bot, position, order, symbolInfo.LastPrice, symbolInfo, position.CurrentQuantity, result);
		if (bot.IsTakePofitEnabled) await CreateTakeProfitOrders(bot, position, order, orderResult.EntryPrice, symbolInfo, position.CurrentQuantity, result);

		return result;
	}

	private async Task<bool> CloseShort(ADBot bot, AMOrderRequest request, AMProviderResult result)
	{
		var position = await GetCurrentPosition(request.Ticker, request.PositionType);
		if (position == null)
		{
			result.WithMessage("No open long position found").AddAudit(AuditType.CloseOrderPlaced, $"No open long position found", CorrelationId, data: JsonConvert.SerializeObject(new { request.Ticker, OrderType = "Sell", PositionType = "Long" }));
			return true;
		}

		var order = await GetOrder(request.OrderId);
		if (!request.ValidateCloseRequest(position, order, bot, result))
			return false;

		result.CorrelationId = position.Id.ToString();
		var quantity = position.Quantity;

		if (request.Quantity > 0)
		{
			var symbolInfo = await GetSymbolInfo(request.Ticker);
			if (request.QuantityType == QuantityType.Exact)
				quantity = request.Quantity.Value;
			else
				quantity = Math.Round(position.Quantity * (request.Quantity.Value / 100), symbolInfo.QuantityPrecision);
		}
		var closePosition = order == null || order.ClosePosition;
		var orderResult = await PlaceMarketOrder(request.Ticker, quantity, OrderSide.Buy, PositionSide.Short, result, closePosition);

		await ReducePosition(position, orderResult, order);
		if (!orderResult.Success) return false;

		result.WithSuccess();
		return true;
	}

	private async Task<AMProviderResult> CloseAll(AMOrderRequest order, AMProviderResult result)
	{
		var allPositions = await GetPositions();
		var positions = allPositions
			.Where(x => x.Symbol == order.Ticker && x.Quantity != 0)
			.Select(x => new BinanceFuturesBatchOrder
			{
				Symbol = order.Ticker,
				PositionSide = x.PositionSide,
				Side = x.PositionSide == PositionSide.Long ? OrderSide.Sell : OrderSide.Buy,
				Type = FuturesOrderType.Market,
				Quantity = Math.Abs(x.Quantity)
			})
			.ToArray();

		var closeOrder = await Client.UsdFuturesApi.Trading.PlaceMultipleOrdersAsync(positions);
		for (var i = 0; i < closeOrder.Data.Count(); i++)
		{
			var response = closeOrder.Data.ElementAt(i);
			if (response.Success)
			{
				await ClosePosition(new AMOrderRequest
				{
					Ticker = order.Ticker,
					Type = response.Data.PositionSide == PositionSide.Long ? "close-long" : "close-short"
				});
			}

			result.AddAudit(AuditType.CloseOrderPlaced, response.Success ? $"Placed close order successfully." : $"Failed placing close order: {response?.Error?.Message}.", CorrelationId, JsonConvert.SerializeObject(new { order.Ticker, response?.Data?.Quantity, response?.Data?.AveragePrice, OrderType = response?.Data?.Side.ToString(), PositionType = response?.Data?.PositionSide.ToString() }));
		}

		result.WithSuccess();

		return result;
	}

	private async Task CreateStopLossOrder(ADBot bot, ADPosition position, AMOrderRequest order, decimal entryPrice, AMSymbolInfo symbol, decimal quantity, AMProviderResult result)
	{
		if (position == null) return;

		if (bot.StopLossPrice <= 0)
		{
			result.AddAudit(AuditType.StopLossOrderPlaced, $"Failed placing stop loss order: Stop loss price is zero.", CorrelationId, JsonConvert.SerializeObject(new { order.Ticker, Quantity = quantity, entryPrice }));
			return;
		}

		var stopPrice = GetStopLoss(bot, entryPrice, symbol.PricePrecision, order.PositionType);
		await AddOrder(position, OrderTriggerType.StopLoss, stopPrice!.Value, quantity, true);
	}

	private async Task CreateTakeProfitOrders(ADBot bot, ADPosition position, AMOrderRequest order, decimal entryPrice, AMSymbolInfo symbol, decimal quantity, AMProviderResult result)
	{
		if (position == null) return;

		if (bot.IsPositionSizeExpandable) await CancelTakeProfitOrders(position, order, result);

		if (bot.TakeProfitTargets.Count == 0)
		{
			result.AddAudit(AuditType.TakeProfitOrderPlaced, $"Failed placing take profit order: No target to place order.", CorrelationId, JsonConvert.SerializeObject(new { order.Ticker, Quantity = quantity, entryPrice }));
			return;
		}

		var targets = bot.TakeProfitTargets;
		if (targets.Any(x => !(x.Share > 0) || !(x.Activation > 0)))
		{
			result.AddAudit(AuditType.TakeProfitOrderPlaced, $"Failed placing take profit order: Target(s) share or price value is empty/zero.", CorrelationId, JsonConvert.SerializeObject(new { order.Ticker, Quantity = quantity, entryPrice }));
			return;
		}

		for (var i = 0; i < targets.Count; i++)
		{
			var target = targets[i];
			var qty = Math.Round(quantity * target.Share / 100, symbol.QuantityPrecision);
			var stopPrice = CalculateTakeProfit(target.Activation, entryPrice, symbol.PricePrecision, order.PositionType);
			await AddOrder(position, OrderTriggerType.TakeProfit, stopPrice, qty, i == targets.Count - 1);
			result.AddAudit(AuditType.TakeProfitOrderPlaced, $"Placed take profit order at target {i + 1} successfully.", CorrelationId, JsonConvert.SerializeObject(new { order.Ticker, Quantity = quantity, entryPrice, Activation = stopPrice }));
		}
	}

	private async Task CancelTakeProfitOrders(ADPosition position, AMOrderRequest order, AMProviderResult result)
	{
		var orders = await GetOrders(position, OrderTriggerType.TakeProfit);
		if (orders.Count == 0) return;

		foreach (var o in orders)
		{
			o.Status = Core.OrderStatus.Cancelled;
			o.UpdatedDate = DateTime.UtcNow;
		}

		await Db.SaveChangesAsync();
	}

	private async Task PlaceStopLossOrder(ADBot bot, AMOrderRequest order, decimal entryPrice, AMSymbolInfo symbol, decimal quantity, AMProviderResult result)
	{
		var oSide = order.PositionType == PositionType.Long ? OrderSide.Sell : OrderSide.Buy;
		var pSide = order.PositionType == PositionType.Long ? PositionSide.Long : PositionSide.Short;

		var stopPrice = GetStopLoss(bot, entryPrice, symbol.PricePrecision, order.PositionType);

		var stopOrderResponse = await Client.UsdFuturesApi.Trading
			.PlaceOrderAsync(
				order.Ticker,
				oSide,
				FuturesOrderType.StopMarket,
				quantity,
				positionSide: pSide,
				stopPrice: stopPrice,
				timeInForce: TimeInForce.GoodTillExpiredOrCanceled,
				workingType: WorkingType.Mark,
				closePosition: true
			);
		result.AddAudit(AuditType.StopLossOrderPlaced, stopOrderResponse.Success ? $"Placed stop loss order successfully." : $"Failed placing stop loss order: {stopOrderResponse?.Error?.Message}", CorrelationId, JsonConvert.SerializeObject(new { order.Ticker, Quantity = quantity, entryPrice, Activation = stopPrice }));

		if (bot.StopLossCallbackRate > 0)
		{
			var cRate = bot.StopLossCallbackRate > 0 ? bot.StopLossCallbackRate : null;
			var trailiginStopOrder = await Client.UsdFuturesApi.Trading
				.PlaceOrderAsync(
					order.Ticker,
					oSide,
					FuturesOrderType.TrailingStopMarket,
					quantity,
					positionSide: pSide,
					callbackRate: cRate,
					timeInForce: TimeInForce.GoodTillExpiredOrCanceled,
					workingType: WorkingType.Mark
				);
			result.AddAudit(AuditType.StopLossOrderPlaced, trailiginStopOrder.Success ? $"Placed trailing stop loss order successfully." : $"Failed placing trailing stop loss order: {trailiginStopOrder?.Error?.Message}", CorrelationId, JsonConvert.SerializeObject(new { order.Ticker, Quantity = quantity, entryPrice, Activation = stopPrice }));
		}
	}

	private async Task PlaceTakeProfitOrders(ADBot bot, AMOrderRequest order, decimal entryPrice, AMSymbolInfo symbol, decimal quantity, AMProviderResult result)
	{
		if (bot.IsPositionSizeExpandable) await CancelCurrentTakeProfitOrders(order, result);

		var targets = bot.TakeProfitTargets;
		if (targets.Any(x => !(x.Share > 0) || !(x.Activation > 0)))
		{
			result.AddAudit(AuditType.TakeProfitOrderPlaced, $"Failed placing take profit order: Target(s) share or price value is empty/zero.", CorrelationId, JsonConvert.SerializeObject(new { order.Ticker, Quantity = quantity, entryPrice }));
			return;
		}

		if (targets.Count == 0) targets.Add(new TakeProfitTarget { Activation = bot.StopLossPrice!.Value * 3, Share = 100 });

		var oSide = order.PositionType == PositionType.Long ? OrderSide.Sell : OrderSide.Buy;
		var pSide = order.PositionType == PositionType.Long ? PositionSide.Long : PositionSide.Short;

		var tpOrders = targets
			.SkipLast(1)
			.Select(x => new BinanceFuturesBatchOrder
			{
				Symbol = order.Ticker,
				Side = oSide,
				PositionSide = pSide,
				Type = FuturesOrderType.TakeProfitMarket,
				Quantity = Math.Round(quantity * x.Share / 100, symbol.QuantityPrecision),
				StopPrice = CalculateTakeProfit(x.Activation, entryPrice, symbol.PricePrecision, order.PositionType),
				TimeInForce = TimeInForce.GoodTillExpiredOrCanceled,
				WorkingType = WorkingType.Contract
			})
			.ToArray();

		if (tpOrders.Length > 0)
		{
			var profitOrderResponse = await Client.UsdFuturesApi.Trading.PlaceMultipleOrdersAsync(tpOrders);
			for (var i = 0; i < profitOrderResponse.Data.Count(); i++)
			{
				var response = profitOrderResponse.Data.ElementAt(i);
				result.AddAudit(AuditType.TakeProfitOrderPlaced, response.Success ? $"Placed take profit order at target {i + 1} successfully." : $"Failed placing take profit order at target {i + 1}: {response?.Error?.Message}", CorrelationId, JsonConvert.SerializeObject(new { order.Ticker, Quantity = quantity, entryPrice, Activation = response?.Data?.StopPrice }));
			}
		}

		// TODO: Last TP order should be closePosition=true but multiple orders does not support it. Find a workaround.
		var closeTp = targets.Last();
		var closeTpOrderResponse = await Client.UsdFuturesApi.Trading
			.PlaceOrderAsync(
				order.Ticker,
				oSide,
				FuturesOrderType.TakeProfitMarket,
				null,
				positionSide: pSide,
				stopPrice: CalculateTakeProfit(closeTp.Activation, entryPrice, symbol.PricePrecision, order.PositionType),
				timeInForce: TimeInForce.GoodTillExpiredOrCanceled,
				workingType: WorkingType.Contract,
				closePosition: true
			);
		result.AddAudit(AuditType.TakeProfitOrderPlaced, closeTpOrderResponse.Success ? $"Placed take profit order at target {targets.Count()} successfully." : $"Failed placing take profit order at target {targets.Count() + 1}: {closeTpOrderResponse?.Error?.Message}", CorrelationId, JsonConvert.SerializeObject(new { order.Ticker, Quantity = quantity, entryPrice, Activation = closeTpOrderResponse?.Data?.StopPrice }));
	}

	private async Task CancelCurrentTakeProfitOrders(AMOrderRequest order, AMProviderResult result)
	{
		var ordersResponse = await Client.UsdFuturesApi.Trading.GetOpenOrdersAsync(order.Ticker);
		if (!ordersResponse.Success)
			throw new Exception($"Failed getting open orders: {ordersResponse?.Error?.Message}");

		var existTpOrders = ordersResponse.Data.Where(x => x.Symbol == order.Ticker && x.Type == FuturesOrderType.TakeProfitMarket);
		if (!existTpOrders.Any()) return;

		var cancelResult = await Client.UsdFuturesApi.Trading.CancelMultipleOrdersAsync(order.Ticker, existTpOrders.Select(x => x.Id).ToList());
		if (!cancelResult.Success)
			throw new Exception($"Failed cancelling open take profit orders: {cancelResult?.Error?.Message}");

		result.AddAudit(AuditType.TakeProfitOrderPlaced, $"Cancelled {cancelResult.Data.Count(x => x.Success)} take profit order(s) out of {cancelResult.Data.Count()}.", CorrelationId, JsonConvert.SerializeObject(new { order.Ticker }));
	}

	private async Task<AMOrderResult> PlaceMarketOrder(string ticker, decimal quantity, OrderSide oSide, PositionSide pSide, AMProviderResult result, bool closePosition = false)
	{
		var orderResponse = await Client.UsdFuturesApi.Trading
			.PlaceOrderAsync(
				ticker,
				oSide,
				FuturesOrderType.Market,
				quantity,
				positionSide: pSide,
				workingType: WorkingType.Contract,
				orderResponseType: OrderResponseType.Result
			);


		if (!orderResponse.Success)
		{
			result.WithMessage($"Failed Market Order Placing Order: {orderResponse?.Error?.Message}").AddAudit(AuditType.OpenOrderPlaced, $"Failed placing market order: {orderResponse?.Error?.Message}", CorrelationId, data: JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
			return new();
		}

		result.WithSuccess().AddAudit(AuditType.OpenOrderPlaced, $"Placed market order successfully.", CorrelationId, JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
		var data = orderResponse.Data;

		return AMOrderResult.WithSuccess(data.AveragePrice, data.QuantityFilled, data.Id.ToString());
	}

	private async Task<AMOrderResult> PlaceLimitOrder(string ticker, decimal quantity, OrderSide oSide, PositionSide pSide, LimitSettings settings, AMProviderResult result)
	{
		if (settings.ValorizationType == ValorizationType.LastPrice)
			return await PlaceDeviatedOrder(ticker, quantity, oSide, pSide, settings, result);

		var (success, orderBook) = await TryGetOrderBook(ticker, result);
		if (!success) return new();

		if (settings.ForceUntilFilled)
			return await PlaceOrderTillPositionFilled(orderBook, ticker, quantity, oSide, pSide, settings, result);

		return await PlaceOboOrder(orderBook, ticker, quantity, oSide, pSide, settings, result);
	}

	private async Task<AMOrderResult> PlaceOrderTillPositionFilled(AMOrderBook orderBook, string ticker, decimal quantity, OrderSide oSide, PositionSide pSide, LimitSettings settings, AMProviderResult result)
	{
		var remainingQuantity = quantity;
		var lastEntryPrice = 0m;
		var totalQuantity = 0m;
		var i = 0;
		var cts = new CancellationTokenSource(settings.ForceTimeout * 1000);
		while (remainingQuantity > 0)
		{
			if (cts.IsCancellationRequested)
			{
				result.AddAudit(AuditType.OpenOrderPlaced, $"Force order book limit order timed out.", CorrelationId, data: JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
				return new();
			}

			var (p, q) = pSide == PositionSide.Long
				? await orderBook.GetBestAsk()
				: await orderBook.GetBestBid();

			if (p <= 0 || q <= 0)
				continue;

			var orderQuantity = Math.Min(remainingQuantity, q);
			var orderResponse = await Client.UsdFuturesApi.Trading
				.PlaceOrderAsync(
					ticker,
					oSide,
					FuturesOrderType.Limit,
					orderQuantity,
					p,
					positionSide: pSide,
					timeInForce: TimeInForce.FillOrKill,
					workingType: WorkingType.Contract,
					orderResponseType: OrderResponseType.Result
				);

			totalQuantity += orderResponse.Data.QuantityFilled;
			i++;

			if (!orderResponse.Success || orderResponse.Data.AveragePrice <= 0)
			{
				await Task.Delay(300);
				result.AddAudit(AuditType.OpenOrderPlaced, $"Failed placing force order book limit order at try {i}: {orderResponse?.Error?.Message}", CorrelationId, data: JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = orderQuantity, EntryPrice = p, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
				continue;
			}

			lastEntryPrice = orderResponse.Data.AveragePrice;
			remainingQuantity -= orderResponse.Data.QuantityFilled;
			result.AddAudit(AuditType.OpenOrderPlaced, $"Placed force order book limit order at at try {i} successfully.", CorrelationId, JsonConvert.SerializeObject(new { Ticker = ticker, EntryPrice = p, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
		}

		result.WithSuccess().AddAudit(AuditType.OpenOrderPlaced, $"Placed successfully.", CorrelationId, JsonConvert.SerializeObject(new { Ticker = ticker, LastEntryPrice = lastEntryPrice, AveragePrice = lastEntryPrice / i, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
		return AMOrderResult.WithSuccess(lastEntryPrice, totalQuantity);
	}

	private async Task<AMOrderResult> PlaceDeviatedOrder(string ticker, decimal quantity, OrderSide oSide, PositionSide pSide, LimitSettings settings, AMProviderResult result)
	{
		var symbolInfo = await GetSymbolInfo(ticker);
		var deviatedDifference = symbolInfo.LastPrice * settings.Deviation / 100;

		var price = pSide == PositionSide.Long ? Math.Round(symbolInfo.LastPrice + deviatedDifference, symbolInfo.PricePrecision - 1) : Math.Round(symbolInfo.LastPrice - deviatedDifference, symbolInfo.PricePrecision - 1);
		var orderResponse = await Client.UsdFuturesApi.Trading
			.PlaceOrderAsync(
				ticker,
				oSide,
				FuturesOrderType.Limit,
				quantity,
				price,
				positionSide: pSide,
				timeInForce: TimeInForce.FillOrKill,
				workingType: WorkingType.Contract,
				orderResponseType: OrderResponseType.Result
			);

		if (!orderResponse.Success)
		{
			result.WithMessage($"Failed placing limit order: {orderResponse?.Error?.Message}").AddAudit(AuditType.OpenOrderPlaced, $"Failed placing limit order: {orderResponse?.Error?.Message}", CorrelationId, data: JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, EntryPrice = price, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
			return new();
		}

		result.WithSuccess().AddAudit(AuditType.OpenOrderPlaced, $"Placed limit order successfully.", CorrelationId, JsonConvert.SerializeObject(new { Ticker = ticker, EntryPrice = price, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));

		var data = orderResponse.Data;
		return AMOrderResult.WithSuccess(data.AveragePrice, data.QuantityFilled, data.Id.ToString());
	}

	private async Task<AMOrderResult> PlaceOboOrder(AMOrderBook orderBook, string ticker, decimal quantity, OrderSide oSide, PositionSide pSide, LimitSettings settings, AMProviderResult result)
	{
		var i = 0;
		if (settings.ComputeEntryPoint) i = await GetEntryPointIndex(orderBook, pSide == PositionSide.Long ? PositionType.Long : PositionType.Short, settings);

		var endIndex = settings.OrderBookOffset + i;
		while (i < endIndex)
		{
			var (p, _) = pSide == PositionSide.Long ? await orderBook.GetNthBestAsk(settings.OrderBookSkip + i) : await orderBook.GetNthBestBid(settings.OrderBookSkip + i);
			var orderResponse = await Client.UsdFuturesApi.Trading
				.PlaceOrderAsync(
					ticker,
					oSide,
					FuturesOrderType.Limit,
					quantity,
					p,
					positionSide: pSide,
					timeInForce: TimeInForce.FillOrKill,
					workingType: WorkingType.Contract,
					orderResponseType: OrderResponseType.Result
				);

			// var openPosition = await GetPosition(ticker, pSide.ToPositionType());
			// if (openPosition != null)
			if (orderResponse.Success)
			{
				result.WithSuccess().AddAudit(AuditType.OpenOrderPlaced, $"Placed OBO limit order at {i + 1} successfully.", CorrelationId, JsonConvert.SerializeObject(new { Ticker = ticker, EntryPrice = p, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
				var data = orderResponse.Data;

				return AMOrderResult.WithSuccess(data.AveragePrice, data.QuantityFilled, data.Id.ToString());
			}

			result.WithMessage($"Failed placing OBO limit order at {i + 1}: {orderResponse?.Error?.Message}").AddAudit(AuditType.OpenOrderPlaced, $"Failed placing OBO limit order at {i + 1}: {orderResponse?.Error?.Message}", CorrelationId, data: JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, EntryPrice = p, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
			i++;
		}

		return new();
	}

	private async Task<(bool, AMOrderBook)> TryGetOrderBook(string ticker, AMProviderResult result)
	{
		var ob = await GetOrderBook(ticker);
		if (await ob.ReadLastUpdateTime() > 0)
		{
			return (true, ob);
		}

		var orderBookResponse = await Client.UsdFuturesApi.ExchangeData.GetOrderBookAsync(ticker);
		if (!orderBookResponse.Success)
		{
			result.WithMessage($"Failed getting order book: {orderBookResponse?.Error?.Message}").AddAudit(AuditType.OpenOrderPlaced, $"Failed getting order book: {orderBookResponse?.Error?.Message}", CorrelationId, data: JsonConvert.SerializeObject(new { Ticker = ticker }));
			return (false, ob);
		}

		await ob.LoadSnapshot(orderBookResponse.Data.Asks, orderBookResponse.Data.Bids, 1);

		return (true, ob);
	}

	private async Task<ADPosition?> GetCurrentPosition(string ticker, PositionType side)
	{
		var position = await Db.Positions
			.Include(x => x.Bot)
			.Include(x => x.Orders)
			.Where(x => x.ExchangeId == Exchange.Id && x.Status == PositionStatus.Open)
			.FirstOrDefaultAsync(x => x.Symbol == ticker && x.Type == side);

		return position;
	}

	private async Task<IEnumerable<ADPosition>> GetCurrentPositions()
	{
		var positions = await Db.Positions
			.Where(x => x.ExchangeId == Exchange.Id && x.Status == PositionStatus.Open)
			.ToListAsync();

		return positions;
	}

	private async Task<ADOrder?> GetOrder(Guid? orderId)
	{
		if (!orderId.HasValue) return null;

		var order = await Db.Orders
			.Include(x => x.Position)
			.FirstOrDefaultAsync(x => x.Id == orderId);

		return order;
	}

	private async Task<IEnumerable<BinancePositionDetailsUsdt>> GetPositions()
	{
		var response = await Client.UsdFuturesApi.Account.GetPositionInformationAsync();
		if (!response.Success)
			throw new Exception($"Failed getting position information: {response?.Error?.Message}");

		return response.Data;
	}

	private async Task<decimal> ConvertUsdtToCoin(ADBot bot, AMOrderRequest order)
	{
		if (order.Quantity > 0) bot.PositionSize = order.Quantity;

		var symbolInfo = await GetSymbolInfo(order.Ticker);
		if (bot.PositionSizeType == PositionSizeType.FixedInAsset) return Math.Round(bot.PositionSize!.Value, symbolInfo.QuantityPrecision);

		symbolInfo = await GetSymbolInfo(order.Ticker);

		// TODO: Dynamic position size calculation
		// if (!bot.PositionSize.HasValue || bot.PositionSize <= 0)
		// {
		// 	if (bot.StopLossPrice == 0 || order.Leverage == 0) throw new Exception("Stoploss or leverage is not set");

		// 	bot.PositionSize = wallet / bot.StopLossPrice / order.Leverage;

		// 	return Math.Round(bot.PositionSize!.Value / symbolInfo.LastPrice, symbolInfo.QuantityPrecision);
		// }
		// order.Leverage = order.Leverage <= 0 ? (int)(wallet / bot.PositionSize!.Value / bot.StopLossPrice!.Value) : order.Leverage;

		if (bot.PositionSizeType == PositionSizeType.FixedInUsd) return Math.Round(bot.PositionSize!.Value / symbolInfo.LastPrice, symbolInfo.QuantityPrecision);

		var balancesResponse = Client.UsdFuturesApi.Account.GetBalancesAsync().GetAwaiter().GetResult();
		if (!balancesResponse.Success) throw new Exception($"Could not get account balances: {balancesResponse?.Error?.Message}");

		var usdtBalanceInfo = balancesResponse.Data.FirstOrDefault(x => x.Asset == "USDT") ?? throw new Exception("Could not find USDT balance");
		var wallet = usdtBalanceInfo.AvailableBalance / order.Risk;

		var usdQuantity = usdtBalanceInfo.AvailableBalance * Convert.ToDecimal(bot.PositionSize) / 100;

		return Math.Round(usdQuantity / symbolInfo.LastPrice, symbolInfo.QuantityPrecision);
	}

	private static List<BinanceOrderBookEntry> IncreaseTickSize(IEnumerable<BinanceOrderBookEntry> bids, int precision)
	{
		Dictionary<decimal, BinanceOrderBookEntry> aggregated_bids = new();
		var tickSize = 1.0m / (decimal)Math.Pow(10, precision - 1);

		foreach (var bid in bids)
		{
			var rounded_price = bid.Price - (bid.Price % tickSize);

			if (aggregated_bids.TryGetValue(rounded_price, out var value))
			{
				value.Quantity += bid.Quantity;
			}
			else
			{
				aggregated_bids[rounded_price] = new BinanceOrderBookEntry
				{
					Price = rounded_price,
					Quantity = bid.Quantity
				};
			}
		}

		var smoothed_bids = new List<BinanceOrderBookEntry>(aggregated_bids.Values);

		return smoothed_bids;
	}

	private static decimal? GetStopLoss(ADBot bot, decimal entryPrice, int precision, PositionType type)
	{
		if (!bot.IsStopLossEnabled) return null;

		if (bot.StopLossPrice == null || bot.StopLossPrice <= 0) bot.StopLossPrice = 1;

		return CalculateStopLoss(bot.StopLossPrice.Value, entryPrice, precision, type);
	}

	private static decimal CalculateStopLoss(decimal activation, decimal entryPrice, int precision, PositionType type)
	{
		if (type == PositionType.Long) return Math.Round(entryPrice - (entryPrice * activation / 100), precision);

		return Math.Round(entryPrice + (entryPrice * activation / 100), precision);
	}

	private static decimal CalculateTakeProfit(decimal activation, decimal entryPrice, int precision, PositionType type)
	{
		if (type == PositionType.Long) return Math.Round(entryPrice + (entryPrice * activation / 100), precision);

		return Math.Round(entryPrice - (entryPrice * activation / 100), precision);
	}

	private async Task<AMSymbolInfo> GetSymbolInfo(string ticker)
	{
		var providerName = IsTestNet ? $"{Exchange.Provider.Name}-test" : Exchange.Provider.Name;
		var symbolInfo = await ExchangeStore.GetSymbolInfo(providerName, ticker) ?? throw new Exception($"Could not find symbol info for {ticker}");
		return symbolInfo;
	}

	private async Task<AMOrderBook> GetOrderBook(string ticker)
	{
		var providerName = IsTestNet ? $"{Exchange.Provider.Name}-test" : Exchange.Provider.Name;
		var symbolInfo = await ExchangeStore.GetOrderBook(providerName, ticker);
		return symbolInfo;
	}

	public override async Task<AMProviderResult> ChangeTickersMarginType(List<string> tickers, MarginType marginType)
	{
		var result = new AMProviderResult();
		foreach (var ticker in tickers) await ChangeMarginType(ticker.ToUpper(), marginType, result);

		return result.WithSuccess();
	}

	private async Task ChangeMarginType(string ticker, MarginType marginType, AMProviderResult result)
	{
		var response = await Client.UsdFuturesApi.Account.ChangeMarginTypeAsync(ticker, (FuturesMarginType)marginType);

		if (!response.Success) result.AddAudit(AuditType.ChangeMarginType, $"Could not change margin type for {ticker} to {marginType}: {response?.Error?.Message}", CorrelationId);
		else result.AddAudit(AuditType.ChangeMarginType, $"Changed margin type for {ticker} to {marginType} successfully", CorrelationId);
	}

	public override void Dispose()
	{
		base.Dispose();
		Client.Dispose();
		GC.SuppressFinalize(this);
	}
}
