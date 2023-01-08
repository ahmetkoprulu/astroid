using Astroid.Core;
using Astroid.Entity;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.Logging;

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

	public override void Context(string settings)
	{
		base.Context(settings);

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
		var tickerInfo = await Client.UsdFuturesApi.ExchangeData.GetTickerAsync(order.Ticker);
		if (!tickerInfo.Success) throw new Exception($"Could not get ticker info: {tickerInfo?.Error?.Message}");

		var quantity = ConvertUsdtToCoin(bot.PositionSize, tickerInfo.Data.LastPrice);
		var stopPrice = bot.IsStopLossEnabled ? CalculateStopLoss(bot.ProfitActivation, tickerInfo.Data.LastPrice, order.PositionType) : null;
		var profitPrice = bot.IsTakePofitEnabled ? CalculateTakeProfit(bot.ProfitActivation, tickerInfo.Data.LastPrice, order.PositionType) : null;

		if (order.OrderType == OrderType.Buy && order.PositionType == PositionType.Long)
			await OpenLong(order.Ticker, quantity, stopPrice, profitPrice);
		else if (order.OrderType == OrderType.Sell && order.PositionType == PositionType.Long)
			await CloseLong(order.Ticker);
		else if (order.OrderType == OrderType.Sell && order.PositionType == PositionType.Short)
			await OpenShort(order.Ticker, quantity, stopPrice, profitPrice);
		else if (order.OrderType == OrderType.Buy && order.PositionType == PositionType.Short)
			await CloseShort(order.Ticker);
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

	private decimal? CalculateStopLoss(decimal? activation, decimal lastPrice, PositionType type)
	{
		if (activation == null || activation < 5) return null;

		if (type == PositionType.Long) return lastPrice * (1 - activation / 100);

		return lastPrice * (1 + activation / 100);
	}

	private decimal? CalculateTakeProfit(decimal? activation, decimal lastPrice, PositionType type)
	{
		if (activation == null || activation < 5) return null;
		// TODO: Fix calculation in perspective of the entry price
		if (type == PositionType.Long) return lastPrice * (1 + activation / 100);

		return lastPrice * (1 - activation / 100);
	}

	private async Task OpenLong(string ticker, decimal quantity, decimal? stopPrice = null, decimal? profitPrice = null)
	{
		var orderResponse = await Client.UsdFuturesApi.Trading
			.PlaceOrderAsync(
				ticker,
				OrderSide.Buy,
				FuturesOrderType.Market,
				quantity,
				positionSide: PositionSide.Long
			);

		if (!orderResponse.Success) throw new Exception($"Failed Placing Order: {orderResponse?.Error?.Message}");

		if (stopPrice != null)
		{
			var stopOrderResponse = await Client.UsdFuturesApi.Trading
				.PlaceOrderAsync(
					ticker,
					OrderSide.Sell,
					FuturesOrderType.StopMarket,
					null,
					positionSide: PositionSide.Long,
					stopPrice: stopPrice,
					timeInForce: TimeInForce.GoodTillExpiredOrCanceled,
					workingType: WorkingType.Mark,
					reduceOnly: true,
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
					null,
					positionSide: PositionSide.Long,
					stopPrice: stopPrice,
					timeInForce: TimeInForce.GoodTillExpiredOrCanceled,
					workingType: WorkingType.Mark,
					reduceOnly: true,
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
	}

	private async Task OpenShort(string ticker, decimal quantity, decimal? stopPrice = null, decimal? profitPrice = null)
	{
		var orderResponse = await Client.UsdFuturesApi.Trading
			.PlaceOrderAsync(
				ticker,
				OrderSide.Sell,
				FuturesOrderType.Market,
				quantity,
				positionSide: PositionSide.Short
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
					stopPrice: Math.Round(stopPrice.Value, 3),
					timeInForce: TimeInForce.GoodTillExpiredOrCanceled,
					workingType: WorkingType.Mark,
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
					stopPrice: Math.Round(profitPrice.Value, 3),
					timeInForce: TimeInForce.GoodTillExpiredOrCanceled,
					workingType: WorkingType.Mark,
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
