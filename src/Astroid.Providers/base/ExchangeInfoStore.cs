using System.Collections.Concurrent;
using System.Linq;
using Astroid.Core;
using Astroid.Core.Cache;

namespace Astroid.Providers;

public class ExchangeInfoStore
{
	private ICacheService Cache { get; }

	public ExchangeInfoStore(ICacheService cache) => Cache = cache;

	public async Task<AMExchangeInfo?> Get(string key)
	{
		var info = new AMExchangeInfo
		{
			Name = key,
		};

		var symbols = await Cache.GetStartsWith<AMSymbolInfo>($"Symbol:{key}:");
		info.Symbols = symbols.ToList();

		return info;
	}

	public async Task<AMSymbolInfo?> GetSymbolInfo(string exchange, string symbol)
	{
		var info = await Cache.Get<AMSymbolInfo>($"Symbol:{exchange}:{symbol}");
		if (info is null) return null;

		return info;
	}

	public async Task<AMOrderBook> GetOrderBook(string exchange, string symbol)
	{
		var info = await GetSymbolInfo(exchange, symbol);
		if (info is null) return new AMOrderBook(exchange, symbol, Cache);

		return info.GetOrderBook(exchange, Cache);
	}

	public async Task WriteSymbolInfo(string exchange, AMSymbolInfo info) => await Cache.Set($"Symbol:{exchange}:{info.Name}", info);

	public string GetSymbolKey(string exchange, string symbol) => $"Symbol:{exchange}:{symbol}";

	public async Task<List<AMExchangeInfo>> GetAll()
	{
		var exchanges = new List<AMExchangeInfo>();

		var binance = await Get(ACExchanges.BinanceUsdFutures);
		if (binance != null)
			exchanges.Add(binance);

		var binanceTest = await Get(ACExchanges.BinanceUsdFuturesTest);
		if (binanceTest != null)
			exchanges.Add(binanceTest);

		return exchanges;
	}
}
