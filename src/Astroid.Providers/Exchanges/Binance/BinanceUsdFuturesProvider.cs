using Astroid.Core;
using Astroid.Entity;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models;

namespace Astroid.Providers;

public class BinanceUsdFuturesProvider : ExchangeProviderBase
{
	[PropertyMetadata("API Key", Type = PropertyTypes.Text, Required = true, Encrypted = true, Group = "General")]
	public string Key { get; set; } = string.Empty;

	[PropertyMetadata("API Secret", Type = PropertyTypes.Text, Required = true, Encrypted = true, Group = "General")]
	public string Secret { get; set; } = string.Empty;

	[PropertyMetadata("Test Net", Type = PropertyTypes.Boolean, Group = "General")]
	public bool IsTestNet { get; set; }

	private BinanceClient Client { get; set; }

	public BinanceUsdFuturesProvider(IServiceProvider serviceProvider, ADExchange exchange) : base(serviceProvider, exchange) => Context();

	public override void Context()
	{
		base.Context();

		var options = new BinanceClientOptions
		{
			ApiCredentials = new BinanceApiCredentials(Key, Secret)
		};

		if (IsTestNet)
			options.UsdFuturesApiOptions = new BinanceApiClientOptions { BaseAddress = "https://testnet.binancefuture.com" };

		options.LogLevel = LogLevel.Debug;

		Client = new BinanceClient(options);
	}

	public override async Task<AMProviderResult> ExecuteOrder(ADBot bot, AMOrderRequest order)
	{
		var result = new AMProviderResult();
		try
		{
			if (order.OrderType == OrderType.Buy && order.PositionType == PositionType.Long)
			{
				result = await OpenLong(bot, order, result);
			}
			else if (order.OrderType == OrderType.Sell && order.PositionType == PositionType.Long)
			{
				result = await CloseLong(order, result);
			}
			else if (order.OrderType == OrderType.Sell && order.PositionType == PositionType.Short)
			{
				result = await OpenShort(bot, order, result);
			}
			else if (order.OrderType == OrderType.Buy && order.PositionType == PositionType.Short)
			{
				result = await CloseShort(order, result);
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
		IEnumerable<BinancePositionDetailsUsdt>? positions = null;
		if (bot.OrderMode != OrderMode.TwoWay || !bot.IsPositionSizeExpandable)
			positions = await GetPositions();

		if (!bot.IsPositionSizeExpandable)
		{
			var position = await GetPosition(order.Ticker, PositionSide.Long, positions);
			if (position != null)
			{
				result.AddAudit(AuditType.OpenOrderPlaced, $"Position size is not expandable", CorrelationId, data: JsonConvert.SerializeObject(new { order.Ticker, OrderType = "Buy", PositionType = "Long" }));
				return result;
			}
		}

		if (bot.OrderMode != OrderMode.TwoWay)
		{
			var position = await GetPosition(order.Ticker, PositionSide.Short, positions);
			if (bot.OrderMode == OrderMode.OneWay && position != null)
			{
				result.AddAudit(AuditType.OpenOrderPlaced, $"Position already exists for {order.Ticker} - {position.PositionSide}", CorrelationId, data: JsonConvert.SerializeObject(new { order.Ticker, OrderType = "Buy", PositionType = "Long" }));
				return result;
			}
			else if (bot.OrderMode == OrderMode.Swing && position != null)
				await CloseShort(order, result, true, position.Quantity);
		}

		if (!bot.StopLossPrice.HasValue || bot.StopLossPrice <= 0) bot.StopLossPrice = 1;

		var quantity = ConvertUsdtToCoin(bot, order);
		await Client.UsdFuturesApi.Account.ChangeInitialLeverageAsync(order.Ticker, order.Leverage);

		bool success; decimal price;
		if (bot.OrderType == OrderEntryType.Market) (success, price) = await PlaceMarketOrder(order.Ticker, quantity, OrderSide.Buy, PositionSide.Long, result);
		else (success, price) = await PlaceLimitOrder(order.Ticker, quantity, OrderSide.Buy, PositionSide.Long, bot.LimitSettings, result);

		if (!success) return result;

		var symbolInfo = GetSymbolInfo(order.Ticker);
		if (bot.IsStopLossEnabled) await PlaceStopLossOrder(bot, order, symbolInfo.LastPrice, symbolInfo, quantity, result);
		if (bot.IsTakePofitEnabled) await PlaceTakeProfitOrders(bot, order, price, symbolInfo, quantity, result);

		return result;
	}

	private async Task<AMProviderResult> CloseLong(AMOrderRequest order, AMProviderResult result, bool force = false, decimal quantity = 0)
	{
		if (!force)
		{
			var position = await GetPosition(order.Ticker, PositionSide.Long);
			if (position == null)
			{
				result.WithMessage("No open long position found").AddAudit(AuditType.CloseOrderPlaced, $"No open long position found", CorrelationId, data: JsonConvert.SerializeObject(new { order.Ticker, OrderType = "Sell", PositionType = "Long" }));
				return result;
			}

			quantity = position.Quantity;
		}

		var orderInfo = await Client.UsdFuturesApi.Trading
			.PlaceOrderAsync(
				order.Ticker,
				OrderSide.Sell,
				FuturesOrderType.Market,
				Math.Abs(quantity),
				positionSide: PositionSide.Long
			);

		if (!orderInfo.Success)
		{
			result.WithMessage($"Failed Placing Order: {orderInfo?.Error?.Message}").AddAudit(AuditType.CloseOrderPlaced, $"Failed placing order: {orderInfo?.Error?.Message}", CorrelationId, data: JsonConvert.SerializeObject(new { order.Ticker, Quantity = quantity, OrderType = "Sell", PositionType = "Long" }));
			return result;
		}

		if (!force) result.WithSuccess();
		result.WithMessage("Placed close order successfully.").AddAudit(AuditType.CloseOrderPlaced, $"Placed close order successfully.", CorrelationId, data: JsonConvert.SerializeObject(new { order.Ticker, OrderType = "Sell", PositionType = "Long" }));

		return result;
	}

	private async Task<AMProviderResult> OpenShort(ADBot bot, AMOrderRequest order, AMProviderResult result)
	{
		IEnumerable<BinancePositionDetailsUsdt>? positions = null;
		if (bot.OrderMode != OrderMode.TwoWay || !bot.IsPositionSizeExpandable)
			positions = await GetPositions();

		if (!bot.IsPositionSizeExpandable)
		{
			var position = await GetPosition(order.Ticker, PositionSide.Short, positions);
			if (position != null)
			{
				result.AddAudit(AuditType.OpenOrderPlaced, $"Position size is not expandable", CorrelationId, data: JsonConvert.SerializeObject(new { order.Ticker, OrderType = "Sell", PositionType = "Short" }));
				return result;
			}
		}

		if (bot.OrderMode != OrderMode.TwoWay)
		{
			var position = await GetPosition(order.Ticker, PositionSide.Long, positions);
			if (bot.OrderMode == OrderMode.OneWay && position != null)
			{
				result.AddAudit(AuditType.OpenOrderPlaced, $"Position already exists for {order.Ticker} - {position.PositionSide}", CorrelationId, data: JsonConvert.SerializeObject(new { order.Ticker, OrderType = "Buy", PositionType = "Long" }));
				return result;
			}
			else if (bot.OrderMode == OrderMode.Swing && position != null)
			{
				await CloseLong(order, result, true, position.Quantity);
			}
		}

		if (!bot.StopLossPrice.HasValue || bot.StopLossPrice <= 0) bot.StopLossPrice = 1;

		var quantity = ConvertUsdtToCoin(bot, order);
		await Client.UsdFuturesApi.Account.ChangeInitialLeverageAsync(order.Ticker, order.Leverage);

		bool success; decimal price;
		if (bot.OrderType == OrderEntryType.Market) (success, price) = await PlaceMarketOrder(order.Ticker, quantity, OrderSide.Sell, PositionSide.Short, result);
		else (success, price) = await PlaceLimitOrder(order.Ticker, quantity, OrderSide.Sell, PositionSide.Short, bot.LimitSettings, result);

		if (!success) return result;

		var symbolInfo = GetSymbolInfo(order.Ticker);
		if (bot.IsStopLossEnabled) await PlaceStopLossOrder(bot, order, symbolInfo.LastPrice, symbolInfo, quantity, result);
		if (bot.IsTakePofitEnabled) await PlaceTakeProfitOrders(bot, order, price, symbolInfo, quantity, result);

		return result;
	}

	private async Task<AMProviderResult> CloseShort(AMOrderRequest order, AMProviderResult result, bool force = false, decimal quantity = 0)
	{
		if (!force)
		{
			var position = await GetPosition(order.Ticker, PositionSide.Short);
			if (position == null)
			{
				result.WithMessage("No open long position found").AddAudit(AuditType.CloseOrderPlaced, $"No open long position found", CorrelationId, data: JsonConvert.SerializeObject(new { order.Ticker, OrderType = "Buy", PositionType = "Short" }));
				return result;
			}

			quantity = position.Quantity;
		}

		var orderInfo = await Client.UsdFuturesApi.Trading
			.PlaceOrderAsync(
				order.Ticker,
				OrderSide.Buy,
				FuturesOrderType.Market,
				Math.Abs(quantity),
				positionSide: PositionSide.Short
			);

		if (!orderInfo.Success)
		{
			result.WithMessage($"Failed placing close order: {orderInfo?.Error?.Message}.").AddAudit(AuditType.CloseOrderPlaced, $"Failed placing close order: {orderInfo?.Error?.Message}", CorrelationId, data: JsonConvert.SerializeObject(new { order.Ticker, OrderType = "Buy", PositionType = "Short" }));
			return result;
		}

		if (!force) result.WithSuccess();
		result.WithMessage("Placed close order successfully.").AddAudit(AuditType.CloseOrderPlaced, $"Placed close order successfully.", CorrelationId, data: JsonConvert.SerializeObject(new { order.Ticker, OrderType = "Buy", PositionType = "Short" }));

		return result;
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
				workingType: WorkingType.Contract,
				closePosition: true
			);
		result.AddAudit(AuditType.StopLossOrderPlaced, stopOrderResponse.Success ? $"Placed stop loss order successfully." : $"Failed placing stop loss order: {stopOrderResponse?.Error?.Message}", CorrelationId, JsonConvert.SerializeObject(new { order.Ticker, Quantity = quantity, entryPrice, Activation = stopPrice }));

		if (bot.StopLossCallbackRate > 0)
		{
			var cRate = bot.StopLossCallbackRate > 0 ? bot.StopLossCallbackRate : null;
			var activation = CalculateStopActivation(bot, entryPrice, symbol.PricePrecision, order.PositionType);
			var trailiginStopOrder = await Client.UsdFuturesApi.Trading
				.PlaceOrderAsync(
					order.Ticker,
					oSide,
					FuturesOrderType.TrailingStopMarket,
					quantity,
					positionSide: pSide,
					activationPrice: activation,
					callbackRate: cRate,
					timeInForce: TimeInForce.GoodTillExpiredOrCanceled,
					workingType: WorkingType.Contract
				);
			result.AddAudit(AuditType.StopLossOrderPlaced, trailiginStopOrder.Success ? $"Placed stop loss order successfully." : $"Failed placing stop loss order: {trailiginStopOrder?.Error?.Message}", CorrelationId, JsonConvert.SerializeObject(new { order.Ticker, Quantity = quantity, entryPrice, Activation = stopPrice }));
		}
	}

	private async Task PlaceTakeProfitOrders(ADBot bot, AMOrderRequest order, decimal entryPrice, AMSymbolInfo symbol, decimal quantity, AMProviderResult result)
	{
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

	private async Task<(bool, decimal)> PlaceMarketOrder(string ticker, decimal quantity, OrderSide oSide, PositionSide pSide, AMProviderResult result)
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
			return (false, default);
		}

		result.WithSuccess().AddAudit(AuditType.OpenOrderPlaced, $"Placed market order successfully.", CorrelationId, JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));

		return (true, orderResponse.Data.AveragePrice);
	}

	private async Task<(bool, decimal)> PlaceLimitOrder(string ticker, decimal quantity, OrderSide oSide, PositionSide pSide, LimitSettings settings, AMProviderResult result)
	{
		if (settings.ValorizationType == ValorizationType.LastPrice)
			return await PlaceDeviatedOrder(ticker, quantity, oSide, pSide, settings, result);

		var success = TryGetOrderBook(ticker, result, out var orderBook);
		if (!success) return (false, default);

		if (settings.ForceUntilFilled)
			return await PlaceOrderTillPositionFilled(orderBook, ticker, quantity, oSide, pSide, settings, result);

		return await PlaceOboOrder(orderBook, ticker, quantity, oSide, pSide, settings, result);
	}

	private async Task<(bool, decimal)> PlaceOrderTillPositionFilled(AMOrderBook orderBook, string ticker, decimal quantity, OrderSide oSide, PositionSide pSide, LimitSettings settings, AMProviderResult result)
	{
		var remainingQuantity = quantity;
		var lastEntryPrice = 0m;
		var i = 0;
		var cts = new CancellationTokenSource(settings.ForceTimeout * 1000);
		while (remainingQuantity > 0)
		{
			if (cts.IsCancellationRequested)
			{
				result.AddAudit(AuditType.OpenOrderPlaced, $"Force order book limit order timed out.", CorrelationId, data: JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
				return (false, default);
			}

			var (p, q) = pSide == PositionSide.Long
				? orderBook.GetBestAsk()
				: orderBook.GetBestBid();

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
		return (false, lastEntryPrice);
	}

	private async Task<(bool, decimal)> PlaceDeviatedOrder(string ticker, decimal quantity, OrderSide oSide, PositionSide pSide, LimitSettings settings, AMProviderResult result)
	{
		var symbolInfo = GetSymbolInfo(ticker);
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
			return (false, default);
		}

		result.WithSuccess().AddAudit(AuditType.OpenOrderPlaced, $"Placed limit order successfully.", CorrelationId, JsonConvert.SerializeObject(new { Ticker = ticker, EntryPrice = price, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));

		return (true, orderResponse.Data.AveragePrice);
	}

	private async Task<(bool, decimal)> PlaceOboOrder(AMOrderBook orderBook, string ticker, decimal quantity, OrderSide oSide, PositionSide pSide, LimitSettings settings, AMProviderResult result)
	{
		var i = 0;
		if (settings.ComputeEntryPoint) i = GetEntryPointIndex(orderBook, pSide, settings);

		var endIndex = settings.OrderBookOffset + i;
		while (i < endIndex)
		{
			var (p, _) = pSide == PositionSide.Long ? orderBook.GetNthBestAsk(settings.OrderBookSkip + i) : orderBook.GetNthBestBid(settings.OrderBookSkip + i);
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

			var openPosition = await GetPosition(ticker, pSide);
			if (openPosition != null)
			{
				result.WithSuccess().AddAudit(AuditType.OpenOrderPlaced, $"Placed OBO limit order at {i + 1} successfully.", CorrelationId, JsonConvert.SerializeObject(new { Ticker = ticker, EntryPrice = p, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
				return (true, orderResponse.Data.AveragePrice);
			}

			result.WithMessage($"Failed placing OBO limit order at {i + 1}: {orderResponse?.Error?.Message}").AddAudit(AuditType.OpenOrderPlaced, $"Failed placing OBO limit order at {i + 1}: {orderResponse?.Error?.Message}", CorrelationId, data: JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, EntryPrice = p, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
			i++;
		}

		return (false, default);
	}

	private bool TryGetOrderBook(string ticker, AMProviderResult result, out AMOrderBook orderBook)
	{
		var symbolInfo = GetSymbolInfo(ticker);
		if (symbolInfo.OrderBook != null && symbolInfo.OrderBook.LastUpdateTime > 0)
		{
			orderBook = symbolInfo.OrderBook;
			return true;
		}

		orderBook = new AMOrderBook(ticker);
		var orderBookResponse = Client.UsdFuturesApi.ExchangeData.GetOrderBookAsync(ticker).GetAwaiter().GetResult();
		if (!orderBookResponse.Success)
		{
			result.WithMessage($"Failed getting order book: {orderBookResponse?.Error?.Message}").AddAudit(AuditType.OpenOrderPlaced, $"Failed getting order book: {orderBookResponse?.Error?.Message}", CorrelationId, data: JsonConvert.SerializeObject(new { Ticker = ticker }));
			return false;
		}

		orderBook = new AMOrderBook(ticker);
		orderBook.LoadSnapshot(orderBookResponse.Data.Asks, orderBookResponse.Data.Bids, 1);

		return true;
	}

	private async Task<BinancePositionDetailsUsdt?> GetPosition(string ticker, PositionSide side, IEnumerable<BinancePositionDetailsUsdt>? positions = null)
	{
		if (positions == null)
			positions = await GetPositions();

		var position = positions.FirstOrDefault(x => x.Symbol == ticker && x.PositionSide == side && x.Quantity != 0);

		return position;
	}

	private async Task<IEnumerable<BinancePositionDetailsUsdt>> GetPositions()
	{
		var response = await Client.UsdFuturesApi.Account.GetPositionInformationAsync();
		if (!response.Success)
			throw new Exception($"Failed getting position information: {response?.Error?.Message}");

		return response.Data;
	}

	private decimal ConvertUsdtToCoin(ADBot bot, AMOrderRequest order)
	{
		var symbolInfo = GetSymbolInfo(order.Ticker);
		if (bot.PositionSizeType == PositionSizeType.FixedInAsset) return Math.Round(bot.PositionSize!.Value, symbolInfo.QuantityPrecision);

		var balancesResponse = Client.UsdFuturesApi.Account.GetBalancesAsync().GetAwaiter().GetResult();
		if (!balancesResponse.Success) throw new Exception($"Could not get account balances: {balancesResponse?.Error?.Message}");

		var usdtBalanceInfo = balancesResponse.Data.FirstOrDefault(x => x.Asset == "USDT") ?? throw new Exception("Could not find USDT balance");
		var wallet = usdtBalanceInfo.AvailableBalance / order.Risk;

		symbolInfo = GetSymbolInfo(order.Ticker);
		if (!bot.PositionSize.HasValue || bot.PositionSize <= 0)
		{
			if (bot.StopLossPrice == 0 || order.Leverage == 0) throw new Exception("Stoploss or leverage is not set");

			bot.PositionSize = wallet / bot.StopLossPrice / order.Leverage;

			return Math.Round(bot.PositionSize!.Value / symbolInfo.LastPrice, symbolInfo.QuantityPrecision);
		}

		order.Leverage = order.Leverage <= 0 ? (int)(wallet / bot.PositionSize!.Value / bot.StopLossPrice!.Value) : order.Leverage;

		if (bot.PositionSizeType == PositionSizeType.FixedInAsset) return Math.Round(bot.PositionSize!.Value, symbolInfo.QuantityPrecision);

		if (bot.PositionSizeType == PositionSizeType.FixedInUsd) return Math.Round(bot.PositionSize!.Value / symbolInfo.LastPrice, symbolInfo.QuantityPrecision);

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

	private static decimal? CalculateStopActivation(ADBot bot, decimal entryPrice, int precision, PositionType type)
	{
		if (bot.StopLossActivation == null || bot.StopLossActivation <= 0) return null;

		if (type == PositionType.Long) return Math.Round(entryPrice + (entryPrice * bot.StopLossActivation!.Value / 100), precision);

		return Math.Round(entryPrice - (entryPrice * bot.StopLossActivation!.Value / 100), precision);
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

	private AMSymbolInfo GetSymbolInfo(string ticker)
	{
		var symbolInfo = ExchangeInfoStore.GetSymbolInfo(Exchange.Provider.Name, ticker) ?? throw new Exception($"Could not find symbol info for {ticker}");
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
