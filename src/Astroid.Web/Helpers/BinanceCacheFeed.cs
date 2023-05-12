using Astroid.Core;
using Astroid.Providers;
using Binance.Net.Clients;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using Newtonsoft.Json;

namespace Astroid.Web;

public static class BinanceCacheFeed
{
	private static BinanceSocketClient SocketClient { get; set; }

	private static BinanceClient Client { get; set; }

	static BinanceCacheFeed()
	{
		var key = Environment.GetEnvironmentVariable("ASTROID_BINANCE_TEST_KEY");
		var secret = Environment.GetEnvironmentVariable("ASTROID_BINANCE_TEST_SECRET");

		if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(secret))
			throw new Exception("Binance credentials not found.");

		var creds = new BinanceApiCredentials(key, secret);

		SocketClient = new BinanceSocketClient(new BinanceSocketClientOptions
		{
			UsdFuturesStreamsOptions = new BinanceSocketApiClientOptions
			{
				BaseAddress = "wss://stream.binancefuture.com",
				ApiCredentials = creds,
			},
			LogLevel = LogLevel.Debug
		});

		Client = new BinanceClient(new BinanceClientOptions
		{
			UsdFuturesApiOptions = new BinanceApiClientOptions
			{
				BaseAddress = "https://testnet.binancefuture.com",
				ApiCredentials = creds,
			},
			LogLevel = LogLevel.Debug
		});
	}

	public static async Task StartSubscriptions()
	{
		await GetExchangeInfo();
		await SocketClient.UsdFuturesStreams.SubscribeToAllTickerUpdatesAsync(data =>
		{
			var markPrices = data.Data;

			foreach (var priceInfo in markPrices)
			{
				var symbolInfo = ExchangeInfoStore.GetSymbolInfo(ACExchanges.BinanceUsdFutures, priceInfo.Symbol);
				if (symbolInfo == null) continue;

				symbolInfo.LastPrice = priceInfo.LastPrice;
			}
		});
	}

	public static async Task StopSubscriptions()
	{
		await SocketClient.UnsubscribeAllAsync();
	}

	public static async Task GetExchangeInfo()
	{
		var info = await Client.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync();
		var exchangeInfo = new AMExchangeInfo
		{
			ModifiedAt = DateTime.UtcNow,
			Symbols = info.Data.Symbols.Select(x => new AMSymbolInfo
			{
				Name = x.Name,
				QuantityPrecision = x.QuantityPrecision,
				PricePrecision = x.PricePrecision,
				TickSize = x.LotSizeFilter?.StepSize,
			}).ToList()
		};

		ExchangeInfoStore.Add(ACExchanges.BinanceUsdFutures, exchangeInfo);
	}
}
