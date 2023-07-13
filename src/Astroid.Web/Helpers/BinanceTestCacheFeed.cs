using Astroid.Core;
using Astroid.Providers;
using Binance.Net.Clients;
using Binance.Net.Objects;
using Binance.Net.Objects.Models.Futures;

namespace Astroid.Web.Cache;

public class BinanceTestCacheFeed : IDisposable
{
	private BinanceSocketClient SocketClient { get; set; }

	private BinanceClient Client { get; set; }

	public BinanceTestCacheFeed()
	{
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
		await SocketClient.UsdFuturesStreams.SubscribeToAllTickerUpdatesAsync(data =>
		{
			var prices = data.Data;

			foreach (var priceInfo in prices)
			{
				var symbolInfo = ExchangeInfoStore.GetSymbolInfo(ACExchanges.BinanceUsdFuturesTest, priceInfo.Symbol);
				if (symbolInfo == null) continue;

				symbolInfo.SetLastPrice(priceInfo.LastPrice);
			}
		});

		await SocketClient.UsdFuturesStreams.SubscribeToAllMarkPriceUpdatesAsync(1000, data =>
		{
			var prices = data.Data;

			foreach (var priceInfo in prices)
			{
				var symbolInfo = ExchangeInfoStore.GetSymbolInfo(ACExchanges.BinanceUsdFuturesTest, priceInfo.Symbol);
				if (symbolInfo == null) continue;

				symbolInfo.SetMarkPrice(priceInfo.MarkPrice);
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

		var symbols = info.Data.Symbols.Select(x =>
		{
			var price = prices.Data.FirstOrDefault(p => p.Symbol == x.Name);
			var markPrice = markPrices.Data.FirstOrDefault(p => p.Symbol == x.Name);

			var symbolInfo = new AMSymbolInfo
			{
				Name = x.Name,
				PricePrecision = x.PricePrecision,
				QuantityPrecision = x.QuantityPrecision,
				LastPrice = price?.Price ?? 0,
				MarkPrice = markPrice?.MarkPrice ?? 0,
				OrderBook = new AMOrderBook(x.Name)
			};

			return symbolInfo;
		})
		.Where(x => x.LastPrice > 0 && x.MarkPrice > 0)
		.ToList();

		var exchangeInfo = new AMExchangeInfo
		{
			Name = "Binance USD Futures Test",
			ModifiedAt = DateTime.UtcNow,
			Symbols = symbols
		};

		ExchangeInfoStore.Add(ACExchanges.BinanceUsdFuturesTest, exchangeInfo);
	}

	public async void GetDepthSnapshot(AMOrderBook orderBook)
	{
		var snapshot = await Client.UsdFuturesApi.ExchangeData.GetOrderBookAsync(orderBook.Symbol, 500);
		orderBook.LoadSnapshot(snapshot.Data.Asks, snapshot.Data.Bids, snapshot.Data.LastUpdateId);
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
