using Astroid.Core;
using Astroid.Providers;
using Binance.Net.Clients;
using Binance.Net.Objects;
using Binance.Net.Objects.Models.Futures;

namespace Astroid.Web.Cache;

public class BinanceTestCacheFeed : IDisposable
{
	private ExchangeInfoStore ExchangeStore { get; set; }
	private BinanceSocketClient SocketClient { get; set; }

	private BinanceClient Client { get; set; }

	public BinanceTestCacheFeed(ExchangeInfoStore exchangeStore)
	{
		ExchangeStore = exchangeStore;
		var key = Environment.GetEnvironmentVariable("ASTROID_BINANCE_TEST_KEY");
		var secret = Environment.GetEnvironmentVariable("ASTROID_BINANCE_TEST_SECRET");

		if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(secret))
			throw new Exception("Binance test credentials not found.");

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

	public async Task StartSubscriptions()
	{
		await GetExchangeInfo();
		await SocketClient.UsdFuturesStreams.SubscribeToAllTickerUpdatesAsync(async data =>
		{
			var prices = data.Data;

			foreach (var priceInfo in prices)
			{
				var symbolInfo = await ExchangeStore.GetSymbolInfo(ACExchanges.BinanceUsdFuturesTest, priceInfo.Symbol);
				if (symbolInfo == null) continue;

				symbolInfo.SetLastPrice(priceInfo.LastPrice);
				await ExchangeStore.WriteSymbolInfo(ACExchanges.BinanceUsdFuturesTest, symbolInfo);
			}
		});

		await SocketClient.UsdFuturesStreams.SubscribeToAllMarkPriceUpdatesAsync(1000, async data =>
		{
			var prices = data.Data;

			foreach (var priceInfo in prices)
			{
				var symbolInfo = await ExchangeStore.GetSymbolInfo(ACExchanges.BinanceUsdFuturesTest, priceInfo.Symbol);
				if (symbolInfo == null) continue;

				if (symbolInfo.MarkPrice == priceInfo.MarkPrice) continue;

				symbolInfo.SetMarkPrice(priceInfo.MarkPrice);
				await ExchangeStore.WriteSymbolInfo(ACExchanges.BinanceUsdFuturesTest, symbolInfo);
			}
		});
	}

	public async Task StopSubscriptions() => await SocketClient.UnsubscribeAllAsync();

	public async Task GetExchangeInfo()
	{
		var info = await Client.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync();
		if (!info.Success) throw new Exception("Failed to get Binance Test exchange info");

		var prices = await Client.UsdFuturesApi.ExchangeData.GetPricesAsync();
		if (!prices.Success) throw new Exception("Failed to get Binance Test prices");

		var markPrices = await Client.UsdFuturesApi.ExchangeData.GetMarkPricesAsync();
		if (!markPrices.Success) throw new Exception("Failed to get Binance Test mark prices");

		foreach (var sym in info.Data.Symbols)
		{
			var price = prices.Data.FirstOrDefault(p => p.Symbol == sym.Name);
			var markPrice = markPrices.Data.FirstOrDefault(p => p.Symbol == sym.Name);

			var symbolInfo = new AMSymbolInfo
			{
				Name = sym.Name,
				PricePrecision = sym.PricePrecision,
				QuantityPrecision = sym.QuantityPrecision,
				LastPrice = price?.Price ?? 0,
				MarkPrice = markPrice?.MarkPrice ?? 0,
			};

			if (!(symbolInfo.LastPrice > 0 && symbolInfo.MarkPrice > 0)) return;

			await ExchangeStore.WriteSymbolInfo(ACExchanges.BinanceUsdFuturesTest, symbolInfo);
		}
	}

	public async Task GetDepthSnapshot(AMOrderBook orderBook)
	{
		var snapshot = await Client.UsdFuturesApi.ExchangeData.GetOrderBookAsync(orderBook.Symbol, 500);
		await orderBook.LoadSnapshot(snapshot.Data.Asks, snapshot.Data.Bids, snapshot.Data.LastUpdateId);
	}

	public async Task<BinanceFuturesOrderBook> GetDepth(string ticker)
	{
		var snapshot = await Client.UsdFuturesApi.ExchangeData.GetOrderBookAsync(ticker, 500);
		return snapshot.Data;
	}

	public void Dispose()
	{
		SocketClient?.UnsubscribeAllAsync().Wait();
		SocketClient?.Dispose();
		Client?.Dispose();
	}
}
