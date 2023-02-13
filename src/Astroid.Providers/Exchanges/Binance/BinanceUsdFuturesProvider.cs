using Astroid.Core;
using Astroid.Entity;
using Astroid.Entity.Extentions;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Astroid.Providers.Extentions;
using Binance.Net.Objects.Models.Futures;

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
				result = await OpenLong(order.Ticker, order.Leverage, bot, result);
			}
			else if (order.OrderType == OrderType.Sell && order.PositionType == PositionType.Long)
			{
				result = await CloseLong(order.Ticker, result);
			}
			else if (order.OrderType == OrderType.Sell && order.PositionType == PositionType.Short)
			{
				result = await OpenShort(order.Ticker, order.Leverage, bot, result);
			}
			else if (order.OrderType == OrderType.Buy && order.PositionType == PositionType.Short)
			{
				result = await CloseShort(order.Ticker, result);
			}
			else throw new InvalidOperationException("Order could not be executed.");
		}
		catch (Exception ex)
		{
			result.WithMessage(ex.Message);
			result.AddAudit(AuditType.UnhandledException, ex.Message, data: JsonConvert.SerializeObject(new { order.Ticker, order.OrderType, order.PositionType }));
		}

		return result;
	}

	private async Task<AMProviderResult> OpenLong(string ticker, int leverage, ADBot bot, AMProviderResult result)
	{
		IEnumerable<BinancePositionDetailsUsdt>? positions = null;
		if (bot.OrderMode != OrderMode.TwoWay || !bot.IsPositionSizeExpandable)
			positions = await GetPositions();

		if (!bot.IsPositionSizeExpandable)
		{
			var position = await GetPosition(ticker, PositionSide.Long, positions);
			if (position != null)
			{
				result.AddAudit(AuditType.OpenOrderPlaced, $"Position size is not expandable", data: JsonConvert.SerializeObject(new { Ticker = ticker, OrderType = "Buy", PositionType = "Long" }));
				return result;
			}
		}

		if (bot.OrderMode != OrderMode.TwoWay)
		{
			var position = await GetPosition(ticker, PositionSide.Short, positions);
			if (bot.OrderMode == OrderMode.OneWay && position != null)
			{
				result.AddAudit(AuditType.OpenOrderPlaced, $"Position already exists for {ticker} - {position.PositionSide}", data: JsonConvert.SerializeObject(new { Ticker = ticker, OrderType = "Buy", PositionType = "Long" }));
				return result;
			}
			else if (bot.OrderMode == OrderMode.Swing && position != null)
				await CloseShort(ticker, result, true, position.Quantity);
		}

		if (leverage > 0)
			await Client.UsdFuturesApi.Account.ChangeInitialLeverageAsync(ticker, leverage);

		var tickerInfo = await Client.UsdFuturesApi.ExchangeData.GetMarkPriceAsync(ticker);
		if (!tickerInfo.Success)
		{
			result.WithMessage($"Failed getting ticker information: {tickerInfo?.Error?.Message}").AddAudit(AuditType.OpenOrderPlaced, $"Failed getting ticker information: {tickerInfo?.Error?.Message}", data: JsonConvert.SerializeObject(new { Ticker = ticker, OrderType = "Buy", PositionType = "Long" }));
			return result;
		}

		var quantity = ConvertUsdtToCoin(bot.PositionSize, bot.PositionSizeType, 3, tickerInfo.Data.MarkPrice);

		bool success;
		if (bot.OrderType == OrderEntryType.Market) success = await PlaceMarketOrder(ticker, quantity, OrderSide.Buy, PositionSide.Long, result);
		else success = await PlaceLimitOrder(ticker, quantity, tickerInfo.Data.MarkPrice, OrderSide.Buy, PositionSide.Long, bot.LimitSettings, result);

		if (!success) return result;

		var openPosition = await GetPosition(ticker, PositionSide.Long);
		if (openPosition == null)
		{
			result.AddAudit(AuditType.OpenOrderPlaced, $"Open position not found. Could not execure TP/SL orders.", data: JsonConvert.SerializeObject(new { Ticker = ticker, OrderType = "Buy", PositionType = "Long" }));
			return result;
		}

		var symbolInfo = GetSymbolInfo(ticker);
		var stopPrice = GetStopLoss(bot, tickerInfo.Data.MarkPrice, leverage, symbolInfo.PricePrecision, PositionType.Long);
		if (stopPrice != null)
		{
			var stopOrderResponse = await Client.UsdFuturesApi.Trading
				.PlaceOrderAsync(
					ticker,
					OrderSide.Sell,
					FuturesOrderType.StopMarket,
					quantity,
					positionSide: PositionSide.Long,
					stopPrice: stopPrice,
					timeInForce: TimeInForce.GoodTillExpiredOrCanceled,
					workingType: WorkingType.Mark,
					priceProtect: true,
					closePosition: true
				);
			result.AddAudit(AuditType.StopLossOrderPlaced, stopOrderResponse.Success ? $"Placed stop loss order successfully." : $"Failed placing stop loss order: {stopOrderResponse?.Error?.Message}", CorrelationId, JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, Activation = stopPrice }));
		}

		var profitPrice = GetTakeProfit(bot, tickerInfo.Data.MarkPrice, leverage, symbolInfo.PricePrecision, PositionType.Long);
		if (profitPrice != null)
		{
			var profitOrderResponse = await Client.UsdFuturesApi.Trading
				.PlaceOrderAsync(
					ticker,
					OrderSide.Sell,
					FuturesOrderType.TakeProfitMarket,
					quantity,
					positionSide: PositionSide.Long,
					stopPrice: profitPrice,
					timeInForce: TimeInForce.GoodTillExpiredOrCanceled,
					workingType: WorkingType.Mark,
					priceProtect: true,
					closePosition: true
				);
			result.AddAudit(AuditType.TakeProfitOrderPlaced, profitOrderResponse.Success ? $"Placed take profit order successfully." : $"Failed placing take profit order: {profitOrderResponse?.Error?.Message}", CorrelationId, JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, Activation = profitPrice }));
		}

		return result;
	}

	private async Task<AMProviderResult> CloseLong(string ticker, AMProviderResult result, bool force = false, decimal quantity = 0)
	{
		if (!force)
		{
			var position = await GetPosition(ticker, PositionSide.Long);
			if (position == null)
			{
				result.WithMessage("No open long position found").AddAudit(AuditType.CloseOrderPlaced, $"No open long position found", data: JsonConvert.SerializeObject(new { Ticker = ticker, OrderType = "Sell", PositionType = "Long" }));
				return result;
			}

			quantity = position.Quantity;
		}

		var orderInfo = await Client.UsdFuturesApi.Trading
			.PlaceOrderAsync(
				ticker,
				OrderSide.Sell,
				FuturesOrderType.Market,
				Math.Abs(quantity),
				positionSide: PositionSide.Long
			);

		if (!orderInfo.Success)
		{
			result.WithMessage($"Failed Placing Order: {orderInfo?.Error?.Message}").AddAudit(AuditType.CloseOrderPlaced, $"Failed placing order: {orderInfo?.Error?.Message}", data: JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, OrderType = "Sell", PositionType = "Long" }));
			return result;
		}

		if (!force) result.WithSuccess();
		result.WithMessage("Placed close order successfully.").AddAudit(AuditType.CloseOrderPlaced, $"Placed close order successfully.", data: JsonConvert.SerializeObject(new { Ticker = ticker, OrderType = "Sell", PositionType = "Long" }));

		return result;
	}

	private async Task<AMProviderResult> OpenShort(string ticker, int leverage, ADBot bot, AMProviderResult result)
	{
		IEnumerable<BinancePositionDetailsUsdt>? positions = null;
		if (bot.OrderMode != OrderMode.TwoWay || !bot.IsPositionSizeExpandable)
			positions = await GetPositions();

		if (!bot.IsPositionSizeExpandable)
		{
			var position = await GetPosition(ticker, PositionSide.Short, positions);
			if (position != null)
			{
				result.AddAudit(AuditType.OpenOrderPlaced, $"Position size is not expandable", data: JsonConvert.SerializeObject(new { Ticker = ticker, OrderType = "Sell", PositionType = "Short" }));
				return result;
			}
		}

		if (bot.OrderMode != OrderMode.TwoWay)
		{
			var position = await GetPosition(ticker, PositionSide.Long, positions);
			if (bot.OrderMode == OrderMode.OneWay && position != null)
			{
				result.AddAudit(AuditType.OpenOrderPlaced, $"Position already exists for {ticker} - {position.PositionSide}", data: JsonConvert.SerializeObject(new { Ticker = ticker, OrderType = "Buy", PositionType = "Long" }));
				return result;
			}
			else if (bot.OrderMode == OrderMode.Swing && position != null)
			{
				await CloseLong(ticker, result, true, position.Quantity);
			}
		}

		if (leverage > 0)
			await Client.UsdFuturesApi.Account.ChangeInitialLeverageAsync(ticker, leverage);

		var tickerInfo = await Client.UsdFuturesApi.ExchangeData.GetMarkPriceAsync(ticker);
		if (!tickerInfo.Success)
		{
			result.WithMessage($"Failed getting ticker information: {tickerInfo?.Error?.Message}").AddAudit(AuditType.OpenOrderPlaced, $"Failed getting ticker information: {tickerInfo?.Error?.Message}", data: JsonConvert.SerializeObject(new { Ticker = ticker }));
			return result;
		}

		var quantity = ConvertUsdtToCoin(bot.PositionSize, bot.PositionSizeType, 3, tickerInfo.Data.MarkPrice);
		bool success;
		if (bot.OrderType == OrderEntryType.Market) success = await PlaceMarketOrder(ticker, quantity, OrderSide.Sell, PositionSide.Short, result);
		else success = await PlaceLimitOrder(ticker, quantity, tickerInfo.Data.MarkPrice, OrderSide.Sell, PositionSide.Short, bot.LimitSettings, result);

		if (!success) return result;

		var openPosition = await GetPosition(ticker, PositionSide.Short);
		if (openPosition == null)
		{
			result.AddAudit(AuditType.OpenOrderPlaced, $"Open position not found. Could not execure TP/SL orders.", data: JsonConvert.SerializeObject(new { Ticker = ticker, OrderType = "Sell", PositionType = "Short" }));
			return result;
		}

		var symbolInfo = GetSymbolInfo(ticker);
		var stopPrice = GetStopLoss(bot, tickerInfo.Data.MarkPrice, leverage, symbolInfo.PricePrecision, PositionType.Short);
		if (stopPrice != null)
		{
			var stopOrderResponse = await Client.UsdFuturesApi.Trading
				.PlaceOrderAsync(
					ticker,
					OrderSide.Buy,
					FuturesOrderType.StopMarket,
					quantity,
					positionSide: PositionSide.Short,
					stopPrice: stopPrice,
					timeInForce: TimeInForce.GoodTillExpiredOrCanceled,
					workingType: WorkingType.Mark,
					priceProtect: true,
					closePosition: true
				);

			result.AddAudit(AuditType.StopLossOrderPlaced, stopOrderResponse.Success ? $"Placed stop loss order successfully." : $"Failed placing stop loss order: {stopOrderResponse?.Error?.Message}", CorrelationId, JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, Activation = stopPrice }));
		}

		var profitPrice = GetTakeProfit(bot, tickerInfo.Data.MarkPrice, leverage, symbolInfo.PricePrecision, PositionType.Short);
		if (profitPrice != null)
		{
			var profitOrderResponse = await Client.UsdFuturesApi.Trading
				.PlaceOrderAsync(
					ticker,
					OrderSide.Buy,
					FuturesOrderType.TakeProfitMarket,
					quantity,
					positionSide: PositionSide.Short,
					stopPrice: profitPrice,
					timeInForce: TimeInForce.GoodTillExpiredOrCanceled,
					workingType: WorkingType.Mark,
					priceProtect: true,
					closePosition: true
				);

			result.AddAudit(AuditType.TakeProfitOrderPlaced, profitOrderResponse.Success ? $"Placed take profit order successfully." : $"Failed placing take profit order: {profitOrderResponse?.Error?.Message}", CorrelationId, JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, Activation = profitPrice }));
		}

		return result;
	}

	private async Task<AMProviderResult> CloseShort(string ticker, AMProviderResult result, bool force = false, decimal quantity = 0)
	{
		if (!force)
		{
			var position = await GetPosition(ticker, PositionSide.Short);
			if (position == null)
			{
				result.WithMessage("No open long position found").AddAudit(AuditType.CloseOrderPlaced, $"No open long position found", data: JsonConvert.SerializeObject(new { Ticker = ticker, OrderType = "Buy", PositionType = "Short" }));
				return result;
			}

			quantity = position.Quantity;
		}

		var orderInfo = await Client.UsdFuturesApi.Trading
			.PlaceOrderAsync(
				ticker,
				OrderSide.Buy,
				FuturesOrderType.Market,
				Math.Abs(quantity),
				positionSide: PositionSide.Short
			);

		if (!orderInfo.Success)
		{
			result.WithMessage($"Failed placing close order: {orderInfo?.Error?.Message}.").AddAudit(AuditType.CloseOrderPlaced, $"Failed placing close order: {orderInfo?.Error?.Message}", data: JsonConvert.SerializeObject(new { Ticker = ticker, OrderType = "Buy", PositionType = "Short" }));
			return result;
		}

		if (!force) result.WithSuccess();
		result.WithMessage("Placed close order successfully.").AddAudit(AuditType.CloseOrderPlaced, $"Placed close order successfully.", data: JsonConvert.SerializeObject(new { Ticker = ticker, OrderType = "Buy", PositionType = "Short" }));

		return result;
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
				workingType: WorkingType.Mark
			);

		if (!orderResponse.Success)
		{
			result.WithMessage($"Failed Market Order Placing Order: {orderResponse?.Error?.Message}").AddAudit(AuditType.OpenOrderPlaced, $"Failed placing market order: {orderResponse?.Error?.Message}", data: JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
			return false;
		}

		result.WithSuccess().AddAudit(AuditType.OpenOrderPlaced, $"Placed market order successfully.", CorrelationId, JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));

		return true;
	}

	private async Task<bool> PlaceLimitOrder(string ticker, decimal quantity, decimal lastPrice, OrderSide oSide, PositionSide pSide, LimitSettings settings, AMProviderResult result)
	{
		decimal price;
		if (settings.ValorizationType == ValorizationType.LastPrice)
		{
			var deviatedDifference = lastPrice * settings.Deviation / 100;

			price = pSide == PositionSide.Long ? Math.Round(lastPrice + deviatedDifference, 1) : Math.Round(lastPrice - deviatedDifference, 1);
			var orderResponse = await Client.UsdFuturesApi.Trading
				.PlaceOrderAsync(
					ticker,
					oSide,
					FuturesOrderType.Limit,
					quantity,
					price,
					positionSide: pSide,
					timeInForce: TimeInForce.FillOrKill,
					workingType: WorkingType.Mark
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
		while (i < settings.OrderBookOffset)
		{
			price = pSide == PositionSide.Long ? orderBook.Data.Asks.ElementAt(i).Price : orderBook.Data.Bids.ElementAt(i).Price;
			var orderResponse = await Client.UsdFuturesApi.Trading
				.PlaceOrderAsync(
					ticker,
					oSide,
					FuturesOrderType.Limit,
					quantity,
					price,
					positionSide: pSide,
					timeInForce: TimeInForce.FillOrKill,
					workingType: WorkingType.Mark
				);

			if (orderResponse.Success)
			{
				result.WithSuccess().AddAudit(AuditType.OpenOrderPlaced, $"Placed OBO limit order at try {i} successfully.", CorrelationId, JsonConvert.SerializeObject(new { Ticker = ticker, EntryPrice = price, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
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

	private decimal ConvertUsdtToCoin(decimal size, PositionSizeType type, int precision, decimal lastPrice)
	{
		if (type == PositionSizeType.FixedInAsset) return Math.Round(size, precision);

		if (type == PositionSizeType.FixedInUsd) return Math.Round(size / lastPrice, precision);

		var balancesResponse = Client.UsdFuturesApi.Account.GetBalancesAsync().GetAwaiter().GetResult();
		if (!balancesResponse.Success) throw new Exception($"Could not get account balances: {balancesResponse?.Error?.Message}");

		var usdtBalanceInfo = balancesResponse.Data.FirstOrDefault(x => x.Asset == "USDT");
		if (usdtBalanceInfo == null) throw new Exception("Could not find USDT balance");

		var usdQuantity = usdtBalanceInfo.AvailableBalance * Convert.ToDecimal(size) / 100;

		return Math.Round(usdQuantity / lastPrice, precision);
	}

	private decimal? GetStopLoss(ADBot bot, decimal entryPrice, int leverage, int precision, PositionType type)
	{
		if (!bot.IsStopLossEnabled) return null;

		if (bot.StopLossActivation == null || bot.StopLossActivation <= 0) throw new Exception("Stop Loss Activation is null or less than 0");

		return CalculateStopLoss(bot.StopLossActivation.Value, entryPrice, leverage, precision, type);
	}

	private decimal? GetTakeProfit(ADBot bot, decimal entryPrice, int leverage, int precision, PositionType type)
	{
		if (!bot.IsTakePofitEnabled) return null;

		if (bot.ProfitActivation == null || bot.ProfitActivation.Value <= 0) throw new Exception("Stop Loss Activation is null or less than 0");

		return CalculateTakeProfit(bot.ProfitActivation.Value, entryPrice, leverage, precision, type);
	}

	private decimal CalculateStopLoss(decimal activation, decimal entryPrice, int leverage, int precision, PositionType type)
	{
		entryPrice /= 100;

		if (type == PositionType.Long) return Math.Round(entryPrice * (100 - activation / leverage), precision);

		return Math.Round(entryPrice * (100 + activation / leverage), precision);
	}

	private decimal CalculateTakeProfit(decimal activation, decimal entryPrice, int leverage, int precision, PositionType type)
	{
		entryPrice /= 100;

		if (type == PositionType.Long) return Math.Round(entryPrice * (100 + activation / leverage), precision);

		return Math.Round(entryPrice * (100 - activation / leverage), precision);
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
				Precision = x.PricePrecision
			}).ToList()
		};

		return exchangeInfo;
	}

	public override void Dispose()
	{
		base.Dispose();
		Client.Dispose();
		GC.SuppressFinalize(this);
	}
}
