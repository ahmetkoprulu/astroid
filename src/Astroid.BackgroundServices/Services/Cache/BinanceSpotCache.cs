using Astroid.Core;
using Astroid.Core.Cache;
using Astroid.Core.MessageQueue;
using Astroid.Entity;
using Astroid.Providers;
using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Options;
using CryptoExchange.Net.Authentication;
using Newtonsoft.Json;

namespace Astroid.BackgroundServices.Cache;

public class BinanceSpotCache : IHostedService
{
	private ExchangeInfoStore ExchangeStore { get; set; }
	private ICacheService Cache { get; set; }
	private ILogger<BinanceSpotCache> Logger { get; set; }
	private BinanceSocketClient SocketClient { get; set; }
	private BinanceRestClient Client { get; set; }
	private IMessageQueue Mq { get; set; }
	private ADExchangeProvider Exchange { get; set; }

	public BinanceSpotCache(ExchangeInfoStore exchangeStore, AstroidDb db, IMessageQueue mq, ICacheService cache, ILogger<BinanceSpotCache> logger)
	{
		Cache = cache;
		ExchangeStore = exchangeStore;
		Logger = logger;
		Mq = mq;
		Exchange = db.ExchangeProviders.First(x => x.Name == ACExchanges.BinanceSpot);

		var key = Environment.GetEnvironmentVariable("ASTROID_BINANCE_KEY");
		var secret = Environment.GetEnvironmentVariable("ASTROID_BINANCE_SECRET");

		if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(secret))
			throw new Exception("Binance test credentials not found.");

		var creds = new ApiCredentials(key, secret);

		SocketClient = new BinanceSocketClient(o =>
		{
			o.ApiCredentials = creds;
			// o.Environment = BinanceEnvironment.Testnet;
		});

		Client = new BinanceRestClient(o =>
		{
			o.ApiCredentials = creds;
			// o.Environment = BinanceEnvironment.Testnet;
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
			await Cache.Set($"ping:{ACExchanges.BinanceSpot}", "test", TimeSpan.FromMilliseconds(1000 * 60 * 11));
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
		await SocketClient.SpotApi.ExchangeData.SubscribeToAllTickerUpdatesAsync(async data =>
		{
			var prices = data.Data;
			var pairsList = new List<KeyValuePair<string, string>>();

			foreach (var priceInfo in prices)
			{
				var symbolInfo = await ExchangeStore.GetSymbolInfo(ACExchanges.BinanceSpot, priceInfo.Symbol);
				if (symbolInfo == null) continue;

				var key = ExchangeStore.GetSymbolKey(ACExchanges.BinanceSpot, symbolInfo.Name);
				pairsList.Add(new KeyValuePair<string, string>(key, priceInfo.LastPrice.ToString()));
			}

			await Cache.SetHashBatch("LastPrice", pairsList);
			var pairs = pairsList.ToDictionary(x => x.Key.Split(':').Last(), x => decimal.Parse(x.Value));
#pragma warning disable 4014
			AQPriceChanges.Publish(Mq, new AQPriceChangeMessage { ExchangeId = Exchange.Id, ExchangeName = ACExchanges.BinanceSpot, Prices = pairs }, "PriceChange");
#pragma warning restore 4014
		});

		// await SocketClient.SpotApi.SubscribeToAllMarkPriceUpdatesAsync(1000, async data =>
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
		var info = await Client.SpotApi.ExchangeData.GetExchangeInfoAsync();
		if (!info.Success) throw new Exception("Failed to get exchange info");

		var prices = await Client.SpotApi.ExchangeData.GetPricesAsync();
		if (!prices.Success) throw new Exception("Failed to get prices");

		// var markPrices = await Client.SpotApi.ExchangeData.GetMarkPricesAsync();
		// if (!markPrices.Success) throw new Exception("Failed to get mark prices");

		foreach (var sym in info.Data.Symbols)
		{
			if (sym.Name.Contains('_') || !sym.Name.EndsWith("USDT")) continue;

			var price = prices.Data.FirstOrDefault(p => p.Symbol == sym.Name);
			// var markPrice = markPrices.Data.FirstOrDefault(p => p.Symbol == sym.Name);

			var symbolInfo = new Dictionary<string, object>
			{
				{ "PricePrecision", sym.QuoteAssetPrecision },
				{ "BaseAsset", sym.BaseAsset },
				{ "QuoteAsset", sym.QuoteAsset },
				{ "QuantityPrecision", sym.BaseAssetPrecision },
				{ "StepSize", sym.LotSizeFilter?.StepSize ?? 0 },
				{ "LastPrice", price?.Price ?? 0 },
				{ "MarkPrice", 0 },
			};

			if (!(price?.Price > 0)) continue;

			await ExchangeStore.WriteSymbolInfo(ACExchanges.BinanceSpot, sym.Name, symbolInfo);
		}
	}

	public async Task GetDepthSnapshot(AMOrderBook orderBook)
	{
		var snapshot = await Client.SpotApi.ExchangeData.GetOrderBookAsync(orderBook.Symbol, 100);
		await orderBook.LoadSnapshot(
			snapshot.Data.Asks.Select(x => new AMOrderBookEntry { Price = x.Price, Quantity = x.Quantity }),
			snapshot.Data.Bids.Select(x => new AMOrderBookEntry { Price = x.Price, Quantity = x.Quantity }),
			snapshot.Data.LastUpdateId
		);
	}

	// public async Task<BinanceFuturesOrderBook> GetDepth(string ticker)
	// {
	// 	var snapshot = await Client.SpotApi.ExchangeData.GetOrderBookAsync(ticker, 100);
	// 	return snapshot.Data;
	// }

	public void Dispose()
	{
		SocketClient?.UnsubscribeAllAsync().Wait();
		SocketClient?.Dispose();
		Client?.Dispose();
	}
}
