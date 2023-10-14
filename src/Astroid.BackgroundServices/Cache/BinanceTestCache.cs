using Astroid.Core;
using Astroid.Core.Cache;
using Astroid.Providers;
using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Objects;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Options;
using CryptoExchange.Net.Authentication;
using Newtonsoft.Json;

namespace Astroid.BackgroundServices.Cache;

public class BinanceTestCache : IHostedService
{
	private ExchangeInfoStore ExchangeStore { get; set; }
	private ICacheService Cache { get; set; }
	private ILogger<BinanceTestCache> Logger { get; set; }
	private BinanceSocketClient SocketClient { get; set; }
	private BinanceRestClient Client { get; set; }

	public BinanceTestCache(ExchangeInfoStore exchangeStore, ICacheService cache, ILogger<BinanceTestCache> logger)
	{
		Cache = cache;
		ExchangeStore = exchangeStore;
		Logger = logger;
		var key = Environment.GetEnvironmentVariable("ASTROID_BINANCE_TEST_KEY");
		var secret = Environment.GetEnvironmentVariable("ASTROID_BINANCE_TEST_SECRET");

		if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(secret))
			throw new Exception("Binance test credentials not found.");

		var creds = new ApiCredentials(key, secret);

		SocketClient = new BinanceSocketClient(o =>
		{
			o.ApiCredentials = creds;
			o.Environment = BinanceEnvironment.Testnet;
		});

		Client = new BinanceRestClient(o =>
		{
			o.ApiCredentials = creds;
			o.Environment = BinanceEnvironment.Testnet;
		});
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		Logger.LogInformation("Starting Cache Service.");
		await StartSubscriptions();
		_ = Task.Run(() => DoJob(cancellationToken), cancellationToken);
	}

	public async Task DoJob(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			await Cache.Set($"ping:{ACExchanges.BinanceUsdFutures}", "test", TimeSpan.FromMilliseconds(1000 * 60 * 11));
			await Task.Delay(1000 * 60 * 10, cancellationToken);
		}
	}

	public async Task StopAsync(CancellationToken cancellationToken)
	{
		Logger.LogInformation("Stopping Cache Service.");
		await SocketClient.UnsubscribeAllAsync();
		Logger.LogInformation("Unsubscribing All Sockets.");
		SocketClient.Dispose();
		Client.Dispose();
	}

	public async Task StartSubscriptions()
	{
		Logger.LogInformation("Subscribing Sockets.");
		await GetExchangeInfo();
		await SocketClient.UsdFuturesApi.SubscribeToAllTickerUpdatesAsync(async data =>
		{
			var prices = data.Data;
			var pairsList = new List<KeyValuePair<string, string>>();

			foreach (var priceInfo in prices)
			{
				var symbolInfo = await ExchangeStore.GetSymbolInfo(ACExchanges.BinanceUsdFuturesTest, priceInfo.Symbol);
				if (symbolInfo == null) continue;

				if (symbolInfo.LastPrice == priceInfo.LastPrice) continue;

				var key = ExchangeStore.GetSymbolKey(ACExchanges.BinanceUsdFuturesTest, symbolInfo.Name);
				symbolInfo.SetLastPrice(priceInfo.LastPrice);
				pairsList.Add(new KeyValuePair<string, string>(key, JsonConvert.SerializeObject(symbolInfo)));
			}

			await Cache.BatchSet(pairsList);
		});

		// await SocketClient.UsdFuturesApi.SubscribeToAllMarkPriceUpdatesAsync(1000, async data =>
		// {
		// 	var prices = data.Data;

		// 	foreach (var priceInfo in prices)
		// 	{
		// 		var symbolInfo = await ExchangeStore.GetSymbolInfo(ACExchanges.BinanceUsdFuturesTest, priceInfo.Symbol);
		// 		if (symbolInfo == null) continue;

		// 		if (symbolInfo.MarkPrice == priceInfo.MarkPrice) continue;

		// 		symbolInfo.SetMarkPrice(priceInfo.MarkPrice);
		// 		await ExchangeStore.WriteSymbolInfo(ACExchanges.BinanceUsdFuturesTest, symbolInfo);
		// 	}
		// });
		Logger.LogInformation("Subscribed Sockets.");
	}

	public async Task StopSubscriptions() => await SocketClient.UnsubscribeAllAsync();

	public async Task GetExchangeInfo()
	{
		var info = await Client.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync();
		if (!info.Success) throw new Exception("Failed to get exchange info");

		var prices = await Client.UsdFuturesApi.ExchangeData.GetPricesAsync();
		if (!prices.Success) throw new Exception("Failed to get prices");

		// var markPrices = await Client.UsdFuturesApi.ExchangeData.GetMarkPricesAsync();
		// if (!markPrices.Success) throw new Exception("Failed to get mark prices");

		foreach (var sym in info.Data.Symbols)
		{
			var price = prices.Data.FirstOrDefault(p => p.Symbol == sym.Name);
			// var markPrice = markPrices.Data.FirstOrDefault(p => p.Symbol == sym.Name);

			var symbolInfo = new AMSymbolInfo
			{
				Name = sym.Name,
				PricePrecision = sym.PricePrecision,
				QuantityPrecision = sym.QuantityPrecision,
				LastPrice = price?.Price ?? 0,
				// MarkPrice = markPrice?.MarkPrice ?? 0,
			};

			if (!(symbolInfo.LastPrice > 0)) continue;

			await ExchangeStore.WriteSymbolInfo(ACExchanges.BinanceUsdFuturesTest, symbolInfo);
		}
	}

	public async Task GetDepthSnapshot(AMOrderBook orderBook)
	{
		var snapshot = await Client.UsdFuturesApi.ExchangeData.GetOrderBookAsync(orderBook.Symbol, 100);
		await orderBook.LoadSnapshot(snapshot.Data.Asks, snapshot.Data.Bids, snapshot.Data.LastUpdateId);
	}

	public async Task<BinanceFuturesOrderBook> GetDepth(string ticker)
	{
		var snapshot = await Client.UsdFuturesApi.ExchangeData.GetOrderBookAsync(ticker, 100);
		return snapshot.Data;
	}

	public void Dispose()
	{
		SocketClient?.UnsubscribeAllAsync().Wait();
		SocketClient?.Dispose();
		Client?.Dispose();
	}
}
