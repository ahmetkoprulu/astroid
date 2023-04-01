using Astroid.Core;
using Astroid.Entity;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models;

namespace Astroid.Providers;

public class BinanceUsdFuturesProvider : ExchangeProviderBase
{
	[PropertyMetadata("API Key", Type = PropertyTypes.Text, IsEncrypted = true, Group = "General")]
	public string Key { get; set; } = string.Empty;

	[PropertyMetadata("API Secret", Type = PropertyTypes.Text, Required = true, IsEncrypted = true, Group = "General")]
	public string Secret { get; set; } = string.Empty;

	[PropertyMetadata("Test Net", Type = PropertyTypes.Boolean, Group = "General")]
	public bool IsTestNet { get; set; }

	private BinanceClient Client { get; set; }

	public BinanceUsdFuturesProvider(IServiceProvider serviceProvider, ADExchange exchange) : base(serviceProvider, exchange)
	{
		Context();
	}

	public override void Context()
	{
		base.Context();

		var options = new BinanceClientOptions
		{
			ApiCredentials = new ApiCredentials(Key, Secret)
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

		var tickerInfo = await Client.UsdFuturesApi.ExchangeData.GetPriceAsync(order.Ticker);
		if (!tickerInfo.Success)
		{
			result.WithMessage($"Failed getting ticker information: {tickerInfo?.Error?.Message}").AddAudit(AuditType.OpenOrderPlaced, $"Failed getting ticker information: {tickerInfo?.Error?.Message}", CorrelationId, data: JsonConvert.SerializeObject(new { order.Ticker, OrderType = "Buy", PositionType = "Long" }));
			return result;
		}

		if (!bot.StopLossPrice.HasValue || bot.StopLossPrice <= 0) bot.StopLossPrice = 1;

		var symbolInfo = GetSymbolInfo(order.Ticker);
		var quantity = ConvertUsdtToCoin(bot, order, symbolInfo.QuantityPrecision, tickerInfo.Data.Price);
		await Client.UsdFuturesApi.Account.ChangeInitialLeverageAsync(order.Ticker, order.Leverage);

		bool success;
		if (bot.OrderType == OrderEntryType.Market) success = await PlaceMarketOrder(order.Ticker, quantity, OrderSide.Buy, PositionSide.Long, result);
		else success = await PlaceLimitOrder(order.Ticker, quantity, tickerInfo.Data.Price, symbolInfo.PricePrecision - 1, OrderSide.Buy, PositionSide.Long, bot.LimitSettings, result);

		if (!success) return result;

		var openPosition = await GetPosition(order.Ticker, PositionSide.Long);
		if (openPosition == null)
		{
			result.AddAudit(AuditType.OpenOrderPlaced, $"Open position not found. Could not execure TP/SL orders.", CorrelationId, data: JsonConvert.SerializeObject(new { order.Ticker, OrderType = "Buy", PositionType = "Long" }));
			return result;
		}

		if (bot.IsStopLossEnabled) await PlaceStopLossOrder(bot, order, openPosition, symbolInfo, quantity, result);
		if (bot.IsTakePofitEnabled) await PlaceTakeProfitOrders(bot, order, openPosition, symbolInfo, quantity, result);

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

		var tickerInfo = await Client.UsdFuturesApi.ExchangeData.GetPriceAsync(order.Ticker);
		if (!tickerInfo.Success)
		{
			result.WithMessage($"Failed getting ticker information: {tickerInfo?.Error?.Message}").AddAudit(AuditType.OpenOrderPlaced, $"Failed getting ticker information: {tickerInfo?.Error?.Message}", CorrelationId, data: JsonConvert.SerializeObject(new { order.Ticker }));
			return result;
		}

		if (!bot.StopLossPrice.HasValue || bot.StopLossPrice <= 0) bot.StopLossPrice = 1;

		var symbolInfo = GetSymbolInfo(order.Ticker);
		var quantity = ConvertUsdtToCoin(bot, order, symbolInfo.QuantityPrecision, tickerInfo.Data.Price);
		await Client.UsdFuturesApi.Account.ChangeInitialLeverageAsync(order.Ticker, order.Leverage);

		bool success;
		if (bot.OrderType == OrderEntryType.Market) success = await PlaceMarketOrder(order.Ticker, quantity, OrderSide.Sell, PositionSide.Short, result);
		else success = await PlaceLimitOrder(order.Ticker, quantity, tickerInfo.Data.Price, symbolInfo.PricePrecision - 1, OrderSide.Sell, PositionSide.Short, bot.LimitSettings, result);

		if (!success) return result;

		var openPosition = await GetPosition(order.Ticker, PositionSide.Short);
		if (openPosition == null)
		{
			result.AddAudit(AuditType.OpenOrderPlaced, $"Open position not found. Could not execure TP/SL orders.", CorrelationId, data: JsonConvert.SerializeObject(new { order.Ticker, OrderType = "Sell", PositionType = "Short" }));
			return result;
		}

		if (bot.IsStopLossEnabled) await PlaceStopLossOrder(bot, order, openPosition, symbolInfo, quantity, result);
		if (bot.IsTakePofitEnabled) await PlaceTakeProfitOrders(bot, order, openPosition, symbolInfo, quantity, result);

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

	private async Task PlaceStopLossOrder(ADBot bot, AMOrderRequest order, BinancePositionDetailsUsdt position, AMSymbolInfo symbol, decimal quantity, AMProviderResult result)
	{
		var oSide = order.PositionType == PositionType.Long ? OrderSide.Sell : OrderSide.Buy;
		var pSide = order.PositionType == PositionType.Long ? PositionSide.Long : PositionSide.Short;

		var stopPrice = GetStopLoss(bot, position.EntryPrice, symbol.PricePrecision, order.PositionType);

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
				priceProtect: true,
				closePosition: true
			);
		result.AddAudit(AuditType.StopLossOrderPlaced, stopOrderResponse.Success ? $"Placed stop loss order successfully." : $"Failed placing stop loss order: {stopOrderResponse?.Error?.Message}", CorrelationId, JsonConvert.SerializeObject(new { order.Ticker, Quantity = quantity, position.EntryPrice, Activation = stopPrice }));

		if (bot.StopLossCallbackRate > 0)
		{
			var cRate = bot.StopLossCallbackRate > 0 ? bot.StopLossCallbackRate : null;
			var activation = CalculateStopActivation(bot, position.EntryPrice, symbol.PricePrecision, order.PositionType);
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
					workingType: WorkingType.Contract,
					priceProtect: true
				);
			result.AddAudit(AuditType.StopLossOrderPlaced, trailiginStopOrder.Success ? $"Placed stop loss order successfully." : $"Failed placing stop loss order: {trailiginStopOrder?.Error?.Message}", CorrelationId, JsonConvert.SerializeObject(new { order.Ticker, Quantity = quantity, position.EntryPrice, Activation = stopPrice }));
		}
	}

	private async Task PlaceTakeProfitOrders(ADBot bot, AMOrderRequest order, BinancePositionDetailsUsdt position, AMSymbolInfo symbol, decimal quantity, AMProviderResult result)
	{
		var targets = bot.TakeProfitTargets;
		if (targets.Count == 0) targets.Add(new TakeProfitTarget { Activation = bot.StopLossPrice!.Value * 3, Share = 100 });

		var oSide = order.PositionType == PositionType.Long ? OrderSide.Sell : OrderSide.Buy;
		var pSide = order.PositionType == PositionType.Long ? PositionSide.Long : PositionSide.Short;

		for (int i = 0; i < targets.Count; i++)
		{
			var tpTarget = targets[i];
			if (!(tpTarget.Share > 0) || !(tpTarget.Activation > 0))
			{
				result.AddAudit(AuditType.TakeProfitOrderPlaced, $"Failed placing take profit order at target {i + 1}: Share or price value is empty/zero.", CorrelationId, JsonConvert.SerializeObject(new { order.Ticker, Quantity = quantity, position.EntryPrice }));
				continue;
			}

			var closePosition = i + 1 == targets.Count;
			var qShare = Math.Round(quantity * tpTarget.Share.Value / 100, symbol.QuantityPrecision);
			var tpPrice = CalculateTakeProfit(tpTarget.Activation!.Value, position.EntryPrice, symbol.PricePrecision, order.PositionType);

			var profitOrderResponse = await Client.UsdFuturesApi.Trading
				.PlaceOrderAsync(
					order.Ticker,
					oSide,
					FuturesOrderType.TakeProfitMarket,
					qShare,
					positionSide: pSide,
					stopPrice: tpPrice,
					timeInForce: TimeInForce.GoodTillExpiredOrCanceled,
					workingType: WorkingType.Contract,
					priceProtect: true,
					closePosition: closePosition
				);

			result.AddAudit(AuditType.TakeProfitOrderPlaced, profitOrderResponse.Success ? $"Placed take profit order at target {i + 1} successfully." : $"Failed placing take profit order at target {i + 1}: {profitOrderResponse?.Error?.Message}", CorrelationId, JsonConvert.SerializeObject(new { order.Ticker, Quantity = quantity, position.EntryPrice, Activation = tpPrice }));
		}
	}

	private async Task<bool> PlaceMarketOrder(string ticker, decimal quantity, OrderSide oSide, PositionSide pSide, AMProviderResult result)
	{
		var orderResponse = await Client.UsdFuturesApi.Trading
			.PlaceOrderAsync(
				ticker,
				oSide,
				FuturesOrderType.Market,
				quantity,
				positionSide: pSide,
				workingType: WorkingType.Contract
			);

		if (!orderResponse.Success)
		{
			result.WithMessage($"Failed Market Order Placing Order: {orderResponse?.Error?.Message}").AddAudit(AuditType.OpenOrderPlaced, $"Failed placing market order: {orderResponse?.Error?.Message}", CorrelationId, data: JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
			return false;
		}

		result.WithSuccess().AddAudit(AuditType.OpenOrderPlaced, $"Placed market order successfully.", CorrelationId, JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));

		return true;
	}

	private async Task<bool> PlaceLimitOrder(string ticker, decimal quantity, decimal lastPrice, int precision, OrderSide oSide, PositionSide pSide, LimitSettings settings, AMProviderResult result)
	{
		decimal price;
		if (settings.ValorizationType == ValorizationType.LastPrice)
		{
			var deviatedDifference = lastPrice * settings.Deviation / 100;

			price = pSide == PositionSide.Long ? Math.Round(lastPrice + deviatedDifference, precision) : Math.Round(lastPrice - deviatedDifference, precision);
			var orderResponse = await Client.UsdFuturesApi.Trading
				.PlaceOrderAsync(
					ticker,
					oSide,
					FuturesOrderType.Limit,
					quantity,
					price,
					positionSide: pSide,
					timeInForce: TimeInForce.ImmediateOrCancel,
					workingType: WorkingType.Contract
				);

			if (!orderResponse.Success)
			{
				result.WithMessage($"Failed Placing Limit Order: {orderResponse?.Error?.Message}").AddAudit(AuditType.OpenOrderPlaced, $"Failed placing limit order: {orderResponse?.Error?.Message}", CorrelationId, data: JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, EntryPrice = price, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
				return false;
			}

			result.WithSuccess().AddAudit(AuditType.OpenOrderPlaced, $"Placed limit order successfully.", CorrelationId, JsonConvert.SerializeObject(new { Ticker = ticker, EntryPrice = price, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));

			return true;
		}

		var orderBook = await Client.UsdFuturesApi.ExchangeData.GetOrderBookAsync(ticker);
		if (!orderBook.Success)
		{
			result.WithMessage($"Failed getting order book: {orderBook?.Error?.Message}").AddAudit(AuditType.OpenOrderPlaced, $"Failed getting order book: {orderBook?.Error?.Message}", CorrelationId, data: JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
			return false;
		}

		var i = 0;
		var orderBookPrices = pSide == PositionSide.Long ? orderBook.Data.Asks.Skip(settings.OrderBookSkip) : orderBook.Data.Bids.Skip(settings.OrderBookSkip);
		while (i < settings.OrderBookOffset)
		{
			price = orderBookPrices.ElementAt(i).Price;
			var orderResponse = await Client.UsdFuturesApi.Trading
				.PlaceOrderAsync(
					ticker,
					oSide,
					FuturesOrderType.Limit,
					quantity,
					price,
					positionSide: pSide,
					timeInForce: TimeInForce.ImmediateOrCancel,
					workingType: WorkingType.Contract
				);

			var openPosition = await GetPosition(ticker, pSide);
			if (openPosition != null)
			{
				result.WithSuccess().AddAudit(AuditType.OpenOrderPlaced, $"Placed OBO limit order at try {i + 1} successfully.", CorrelationId, JsonConvert.SerializeObject(new { Ticker = ticker, EntryPrice = price, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
				return true;
			}

			result.WithMessage($"Failed Placing OBO Limit Order at try {i + 1}: {orderResponse?.Error?.Message}").AddAudit(AuditType.OpenOrderPlaced, $"Failed placing OBO limit order at try {i + 1}: {orderResponse?.Error?.Message}", CorrelationId, data: JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, EntryPrice = price, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
			i++;
		}

		return false;
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

	private decimal ConvertUsdtToCoin(ADBot bot, AMOrderRequest order, int precision, decimal lastPrice)
	{
		var balancesResponse = Client.UsdFuturesApi.Account.GetBalancesAsync().GetAwaiter().GetResult();
		if (!balancesResponse.Success) throw new Exception($"Could not get account balances: {balancesResponse?.Error?.Message}");

		var usdtBalanceInfo = balancesResponse.Data.FirstOrDefault(x => x.Asset == "USDT");
		if (usdtBalanceInfo == null) throw new Exception("Could not find USDT balance");

		var wallet = usdtBalanceInfo.AvailableBalance / order.Risk;

		if (!bot.PositionSize.HasValue || bot.PositionSize <= 0)
		{
			if (bot.StopLossPrice == 0 || order.Leverage == 0) throw new Exception("Stoploss or leverage is not set");

			bot.PositionSize = wallet / bot.StopLossPrice / order.Leverage;

			return Math.Round(bot.PositionSize!.Value / lastPrice, precision);
		}

		order.Leverage = order.Leverage <= 0 ? (int)(wallet / bot.PositionSize!.Value / bot.StopLossPrice!.Value) : order.Leverage;

		if (bot.PositionSizeType == PositionSizeType.FixedInAsset) return Math.Round(bot.PositionSize!.Value, precision);

		if (bot.PositionSizeType == PositionSizeType.FixedInUsd) return Math.Round(bot.PositionSize!.Value / lastPrice, precision);

		var usdQuantity = usdtBalanceInfo.AvailableBalance * Convert.ToDecimal(bot.PositionSize) / 100;

		return Math.Round(usdQuantity / lastPrice, precision);
	}

	static List<BinanceOrderBookEntry> IncreaseTickSize(IEnumerable<BinanceOrderBookEntry> bids, int precision)
	{
		Dictionary<decimal, BinanceOrderBookEntry> aggregated_bids = new();
		var tickSize = 1.0m / (decimal)Math.Pow(10, precision - 1);

		foreach (var bid in bids)
		{
			var rounded_price = bid.Price - (bid.Price % tickSize);

			if (aggregated_bids.ContainsKey(rounded_price))
			{
				aggregated_bids[rounded_price].Quantity += bid.Quantity;
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

	private decimal? GetStopLoss(ADBot bot, decimal entryPrice, int precision, PositionType type)
	{
		if (!bot.IsStopLossEnabled) return null;

		if (bot.StopLossPrice == null || bot.StopLossPrice <= 0) bot.StopLossPrice = 1;

		return CalculateStopLoss(bot.StopLossPrice.Value, entryPrice, precision, type);
	}

	private decimal? CalculateStopActivation(ADBot bot, decimal entryPrice, int precision, PositionType type)
	{
		if (bot.StopLossActivation == null || bot.StopLossActivation <= 0) return null;

		if (type == PositionType.Long) return Math.Round(entryPrice + (entryPrice * bot.StopLossActivation!.Value / 100), precision);

		return Math.Round(entryPrice - (entryPrice * bot.StopLossActivation!.Value / 100), precision);
	}

	private decimal CalculateStopLoss(decimal activation, decimal entryPrice, int precision, PositionType type)
	{
		if (type == PositionType.Long) return Math.Round(entryPrice - (entryPrice * activation / 100), precision);

		return Math.Round(entryPrice + (entryPrice * activation / 100), precision);
	}

	private decimal CalculateTakeProfit(decimal activation, decimal entryPrice, int precision, PositionType type)
	{
		if (type == PositionType.Long) return Math.Round(entryPrice + (entryPrice * activation / 100), precision);

		return Math.Round(entryPrice - (entryPrice * activation / 100), precision);
	}

	private AMSymbolInfo GetSymbolInfo(string ticker)
	{
		var symbolInfo = ExhangeInfoStore.GetSymbolInfo(Exchange.Provider.Name, ticker, GetExchangeInfo);
		if (symbolInfo == null) throw new Exception($"Could not find symbol info for {ticker}");

		return symbolInfo;
	}

	private AMExchangeInfo GetExchangeInfo()
	{
		var response = Client.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync().GetAwaiter().GetResult();
		if (!response.Success) throw new Exception(response?.Error?.Message ?? "Could not get exchange info");

		var exchangeInfo = new AMExchangeInfo
		{
			ModifiedAt = DateTime.UtcNow,
			Symbols = response.Data.Symbols.Select(x => new AMSymbolInfo
			{
				Name = x.Name,
				QuantityPrecision = x.QuantityPrecision,
				PricePrecision = x.PricePrecision,
				TickSize = x.LotSizeFilter?.StepSize,
			}).ToList()
		};

		return exchangeInfo;
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
