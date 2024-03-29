using Astroid.Core;
using Astroid.Core.Cache;
using Astroid.Core.MessageQueue;
using Astroid.Entity;
using Astroid.Providers;
using Binance.Net.Clients;
using Binance.Net.Objects;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Options;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.CommonObjects;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace Astroid.BackgroundServices.Cache;

public class BinanceCache : IHostedService
{
	private ExchangeInfoStore ExchangeStore { get; set; }
	private ICacheService Cache { get; set; }
	private ILogger<BinanceCache> Logger { get; set; }
	private BinanceSocketClient SocketClient { get; set; }
	private BinanceRestClient Client { get; set; }
	private IMessageQueue Mq { get; set; }
	private ADExchangeProvider Exchange { get; set; }

	public BinanceCache(ExchangeInfoStore exchangeStore, AstroidDb db, IMessageQueue mq, ICacheService cache, ILogger<BinanceCache> logger)
	{
		ExchangeStore = exchangeStore;
		Cache = cache;
		Logger = logger;
		Mq = mq;

		Exchange = db.ExchangeProviders.First(x => x.Name == ACExchanges.BinanceUsdFutures);

		var key = Environment.GetEnvironmentVariable("ASTROID_BINANCE_KEY");
		var secret = Environment.GetEnvironmentVariable("ASTROID_BINANCE_SECRET");

		if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(secret))
			throw new Exception("Binance credentials not found.");

		var creds = new ApiCredentials(key, secret);

		SocketClient = new BinanceSocketClient(o =>
		{
			o.ApiCredentials = creds;
		});

		Client = new BinanceRestClient(o =>
		{
			o.ApiCredentials = creds;
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
				var symbolInfo = await ExchangeStore.GetSymbolInfo(ACExchanges.BinanceUsdFutures, priceInfo.Symbol);
				if (symbolInfo == null) continue;

				var key = ExchangeStore.GetSymbolKey(ACExchanges.BinanceUsdFutures, symbolInfo.Name);
				pairsList.Add(new KeyValuePair<string, string>(key, priceInfo.LastPrice.ToString()));
			}

			await Cache.SetHashBatch("LastPrice", pairsList);
			var pairs = pairsList.ToDictionary(x => x.Key.Split(':').Last(), x => decimal.Parse(x.Value));
#pragma warning disable 4014
			AQPriceChanges.Publish(Mq, new AQPriceChangeMessage { ExchangeId = Exchange.Id, ExchangeName = ACExchanges.BinanceUsdFutures, Prices = pairs }, "PriceChange");
#pragma warning restore 4014
		});

		// await SocketClient.UsdFuturesApi.SubscribeToAllMarkPriceUpdatesAsync(1000, async data =>
		// {
		// 	var prices = data.Data;

		// 	foreach (var priceInfo in prices)
		// 	{
		// 		var symbolInfo = await ExchangeStore.GetSymbolInfo(ACExchanges.BinanceUsdFutures, priceInfo.Symbol);
		// 		if (symbolInfo == null) continue;

		// 		if (symbolInfo.MarkPrice == priceInfo.MarkPrice) continue;

		// 		symbolInfo.SetMarkPrice(priceInfo.MarkPrice);
		// 		await ExchangeStore.WriteSymbolInfo(ACExchanges.BinanceUsdFutures, symbolInfo);
		// 	}
		// });

		// var binanceInfo = await ExchangeStore.Get(ACExchanges.BinanceUsdFutures);
		// var symbolsToCache = binanceInfo!.Symbols
		// 	.Where(x => x.Name == "BTCUSDT" || x.Name == "ETHUSDT" || x.Name == "SOLUSDT" || x.Name == "MATICUSDT")
		// 	.ToList();

		// foreach (var ticker in symbolsToCache)
		// {
		// 	await SocketClient.UsdFuturesApi.SubscribeToOrderBookUpdatesAsync(ticker.Name, 500, async data =>
		// 	{
		// 		var orderBook = await ExchangeStore.GetOrderBook(ACExchanges.BinanceUsdFutures, ticker.Name);
		// 		await orderBook.ProcessUpdate(data.Data);
		// 		if (await orderBook.ReadLastUpdateTime() == 0)
		// 		{
		// 			await orderBook.SetLastUpdateTime(-1);
		// 			Console.WriteLine("Getting snapshot");
		// 			await GetDepthSnapshot(orderBook);
		// 		}
		// 	});
		// }
		Logger.LogInformation("Subscribed Sockets.");
	}

	public async Task StopSubscriptions() => await SocketClient.UnsubscribeAllAsync();

	public async Task GetExchangeInfo(CancellationToken cancellationToken = default)
	{
		var info = await Client.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync(cancellationToken);
		if (!info.Success) throw new Exception("Failed to get Binance exchange info");

		var prices = await Client.UsdFuturesApi.ExchangeData.GetPricesAsync(cancellationToken);
		if (!prices.Success) throw new Exception("Failed to get Binance Test prices");

		// var markPrices = await Client.UsdFuturesApi.ExchangeData.GetMarkPricesAsync(cancellationToken);
		// if (!markPrices.Success) throw new Exception("Failed to get Binance Test mark prices");

		foreach (var sym in info.Data.Symbols)
		{
			if (sym.Name.Contains('_') || !sym.Name.EndsWith("USDT")) continue;

			var price = prices.Data.FirstOrDefault(p => p.Symbol == sym.Name);
			// var markPrice = markPrices.Data.FirstOrDefault(p => p.Symbol == sym.Name);

			var symbolInfo = new Dictionary<string, object>
			{
				{ "PricePrecision", sym.PricePrecision },
				{ "BaseAsset", sym.BaseAsset },
				{ "QuoteAsset", sym.QuoteAsset },
				{ "QuantityPrecision", sym.QuantityPrecision },
				{ "LastPrice", price?.Price ?? 0 },
				{ "MarkPrice", 0 },
			};

			await ExchangeStore.WriteSymbolInfo(ACExchanges.BinanceUsdFutures, sym.Name, symbolInfo);
		}
	}

	public async Task GetDepthSnapshot(AMOrderBook orderBook)
	{
		var snapshot = await Client.UsdFuturesApi.ExchangeData.GetOrderBookAsync(orderBook.Symbol, 100);
		await orderBook.LoadSnapshot(
			snapshot.Data.Asks.Select(x => new AMOrderBookEntry { Price = x.Price, Quantity = x.Quantity }),
			snapshot.Data.Bids.Select(x => new AMOrderBookEntry { Price = x.Price, Quantity = x.Quantity }),
			snapshot.Data.LastUpdateId
		);
	}

	// public async Task<BinanceFuturesOrderBook> GetDepth(string ticker)
	// {
	// 	var snapshot = await Client.UsdFuturesApi.ExchangeData.GetOrderBookAsync(ticker, 100);
	// 	return snapshot.Data;
	// }
}
