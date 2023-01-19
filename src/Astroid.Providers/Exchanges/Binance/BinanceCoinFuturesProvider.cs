using Astroid.Core;
using Astroid.Entity;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.Logging;

namespace Astroid.Providers;

public class BinanceCoinFuturesProvider : ExchangeProviderBase
{
	[PropertyMetadata("API Key", Type = PropertyTypes.Text, IsEncrypted = true, Group = "General")]
	public string Key { get; set; } = string.Empty;

	[PropertyMetadata("API Secret", Type = PropertyTypes.Text, Required = true, IsEncrypted = true, Group = "General")]
	public string Secret { get; set; } = string.Empty;

	[PropertyMetadata("Test Net", Type = PropertyTypes.Boolean, Group = "General")]
	public bool IsTestNet { get; set; }

	private BinanceClient Client { get; set; }

	public override void Context(string settings, ADExchange exchange)
	{
		base.Context(settings, exchange);

		var options = new BinanceClientOptions
		{
			ApiCredentials = new ApiCredentials(Key, Secret)
		};

		if (IsTestNet)
			options.UsdFuturesApiOptions = new BinanceApiClientOptions { BaseAddress = "https://testnet.binancefuture.com" };

#if DEBUG
		options.LogLevel = LogLevel.Debug;
#endif

		Client = new BinanceClient(options);
	}

	public override async Task ExecuteOrder(ADBot bot, OrderRequest order)
	{
		if (order.OrderType == OrderType.Buy && order.PositionType == PositionType.Long)
		{
			await OpenLong(order.Ticker, order.Leverage, bot);
		}
		else if (order.OrderType == OrderType.Sell && order.PositionType == PositionType.Long)
		{
			await CloseLong(order.Ticker);
		}
		else if (order.OrderType == OrderType.Sell && order.PositionType == PositionType.Short)
		{
			await OpenShort(order.Ticker, order.Leverage, bot);
		}
		else if (order.OrderType == OrderType.Buy && order.PositionType == PositionType.Short)
		{
			await CloseShort(order.Ticker);
		}
	}

	private async Task OpenLong(string ticker, int leverage, ADBot bot)
	{
		await Client.UsdFuturesApi.Account.ChangeInitialLeverageAsync(ticker, leverage);

		var tickerInfo = await Client.UsdFuturesApi.ExchangeData.GetTickerAsync(ticker);
		if (!tickerInfo.Success) throw new Exception($"Could not get ticker info: {tickerInfo?.Error?.Message}");

		var symbolInfo = GetSymbolInfo(ticker);
		var quantity = ConvertUsdtToCoin(bot.PositionSize, tickerInfo.Data.LastPrice);
		var stopPrice = GetStopLoss(bot, tickerInfo.Data.LastPrice, leverage, symbolInfo.Precision, PositionType.Long);
		var profitPrice = GetTakeProfit(bot, tickerInfo.Data.LastPrice, symbolInfo.Precision, leverage, PositionType.Long);

		var orderResponse = await Client.UsdFuturesApi.Trading
			.PlaceOrderAsync(
				ticker,
				OrderSide.Buy,
				FuturesOrderType.Market,
				quantity,
				positionSide: PositionSide.Long,
				workingType: WorkingType.Contract
			);

		if (!orderResponse.Success) throw new Exception($"Failed Placing Order: {orderResponse?.Error?.Message}");

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
					workingType: WorkingType.Contract,
					priceProtect: true,
					closePosition: true
				);

			if (!stopOrderResponse.Success) throw new Exception($"Failed Placing Stop Order: {stopOrderResponse?.Error?.Message}");
		}

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
					workingType: WorkingType.Contract,
					priceProtect: true,
					closePosition: true
				);

			if (!profitOrderResponse.Success) throw new Exception($"Failed Placing Take Profit Order: {profitOrderResponse?.Error?.Message}");
		}
	}

	private async Task CloseLong(string ticker)
	{
		var resposne = await Client.UsdFuturesApi.Account.GetPositionInformationAsync();
		if (!resposne.Success) throw new Exception(resposne?.Error?.Message ?? "Could not get position information");

		var position = resposne.Data.FirstOrDefault(x => x.Symbol == ticker && x.PositionSide == PositionSide.Long && x.Quantity != 0);
		if (position == null) throw new Exception("No open short position found");

		var orderInfo = await Client.UsdFuturesApi.Trading
			.PlaceOrderAsync(
				ticker,
				OrderSide.Sell,
				FuturesOrderType.Market,
				Math.Abs(position.Quantity),
				positionSide: PositionSide.Long
			);

		if (!orderInfo.Success) throw new Exception($"Failed Placing Order: {orderInfo?.Error?.Message}");
	}

	private async Task OpenShort(string ticker, int leverage, ADBot bot)
	{
		await Client.UsdFuturesApi.Account.ChangeInitialLeverageAsync(ticker, leverage);

		var tickerInfo = await Client.UsdFuturesApi.ExchangeData.GetTickerAsync(ticker);
		if (!tickerInfo.Success) throw new Exception($"Could not get ticker info: {tickerInfo?.Error?.Message}");

		var symbolInfo = GetSymbolInfo(ticker);
		var quantity = ConvertUsdtToCoin(bot.PositionSize, tickerInfo.Data.LastPrice);
		var stopPrice = GetStopLoss(bot, tickerInfo.Data.LastPrice, leverage, symbolInfo.Precision, PositionType.Long);
		var profitPrice = GetTakeProfit(bot, tickerInfo.Data.LastPrice, symbolInfo.Precision, leverage, PositionType.Long);

		var orderResponse = await Client.UsdFuturesApi.Trading
			.PlaceOrderAsync(
				ticker,
				OrderSide.Sell,
				FuturesOrderType.Market,
				quantity,
				positionSide: PositionSide.Short,
				workingType: WorkingType.Contract
			);

		if (!orderResponse.Success) throw new Exception($"Failed Placing Order: {orderResponse?.Error?.Message}");

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
					workingType: WorkingType.Contract,
					priceProtect: true,
					closePosition: true
				);

			if (!stopOrderResponse.Success) throw new Exception($"Failed Placing Stop Order: {stopOrderResponse?.Error?.Message}");
		}

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
					workingType: WorkingType.Contract,
					priceProtect: true,
					closePosition: true
				);

			if (!profitOrderResponse.Success) throw new Exception($"Failed Placing Take Profit Order: {profitOrderResponse?.Error?.Message}");
		}
	}

	private async Task CloseShort(string ticker)
	{
		var resposne = await Client.UsdFuturesApi.Account.GetPositionInformationAsync();
		if (!resposne.Success) throw new Exception(resposne?.Error?.Message ?? "Could not get position information");

		var position = resposne.Data.FirstOrDefault(x => x.Symbol == ticker && x.PositionSide == PositionSide.Short && x.Quantity != 0);
		if (position == null) throw new Exception("No open short position found");

		var orderInfo = await Client.UsdFuturesApi.Trading
			.PlaceOrderAsync(
				ticker,
				OrderSide.Buy,
				FuturesOrderType.Market,
				Math.Abs(position.Quantity),
				positionSide: PositionSide.Short
			);

		if (!orderInfo.Success) throw new Exception($"Failed Placing Order: {orderInfo?.Error?.Message}");

	}

	private decimal ConvertUsdtToCoin(decimal ratio, decimal lastPrice)
	{
		var balancesResponse = Client.UsdFuturesApi.Account.GetBalancesAsync().GetAwaiter().GetResult();
		if (!balancesResponse.Success) throw new Exception($"Could not get account balances: {balancesResponse?.Error?.Message}");

		var usdtBalanceInfo = balancesResponse.Data.FirstOrDefault(x => x.Asset == "USDT");
		if (usdtBalanceInfo == null) throw new Exception("Could not find USDT balance");

		var usdQuantity = usdtBalanceInfo.AvailableBalance * Convert.ToDecimal(ratio) / 100;

		return Math.Round(usdQuantity / lastPrice, 3);
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
		Client.Dispose();
		GC.SuppressFinalize(this);
	}

	// public async Task Swing()
	// {
	// 			var positions = await client.UsdFuturesApi.Account.GetPositionInformationAsync();
	// 	var position = positions.Data.Where(x => x.Symbol == "BTCUSDT").First();
	// 	var tickerInfo = await client.UsdFuturesApi.Market.Get24HrPriceChangeStatisticsAsync("BTCUSDT");

	// 	var profitPrice = tickerInfo.Data.LastPrice * (decimal)1.1;
	// 	var stopPrice = tickerInfo.Data.LastPrice * (decimal)0.9;

	// 	if (position.PositionSide == PositionSide.Long)
	// 	{
	// 		await CloseLong("BTCUSDT", position.PositionAmt);
	// 		await OpenShort("BTCUSDT", position.PositionAmt, stopPrice, profitPrice);
	// 	}
	// 	else if (position.PositionSide == PositionSide.Short)
	// 	{
	// 		await CloseShort("BTCUSDT", position.PositionAmt);
	// 		await OpenLong("BTCUSDT", position.PositionAmt, stopPrice, profitPrice);
	// }
}
