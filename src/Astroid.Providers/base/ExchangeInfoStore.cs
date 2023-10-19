using System.Collections.Concurrent;
using System.Linq;
using Astroid.Core;
using Astroid.Core.Cache;
using Newtonsoft.Json;

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

		var symbols = await Cache.GetHashStartsWith<AMSymbolInfo>($"Symbol:{key}:");
		info.Symbols = symbols.ToList();

		return info;
	}

	public async Task<AMSymbolInfo?> GetSymbolInfoLazy(string exchange, string symbol)
	{
		var key = $"Symbol:{exchange}:{symbol}";
		var exists = await Cache.IsExists(key);
		if (!exists) return null;

		var info = await Cache.GetAllHash<AMSymbolInfo>(key);
		info.Exchange = exchange;
		info.Name = symbol;
		info.Cache = Cache;

		return info;
	}

	public async Task<AMSymbolInfo?> GetSymbolInfo(string exchange, string symbol)
	{
		var key = $"Symbol:{exchange}:{symbol}";
		var exists = await Cache.IsExists(key);
		if (!exists) return null;

		return new AMSymbolInfo(exchange, symbol, Cache);
	}

	public async Task<AMOrderBook> GetOrderBook(string exchange, string symbol)
	{
		var info = await GetSymbolInfo(exchange, symbol);
		if (info is null) return new AMOrderBook(exchange, symbol, Cache);

		return info.GetOrderBook();
	}

	public async Task WriteSymbolInfo(string exchange, string symbol, Dictionary<string, object> info) => await Cache.SetAllHash($"Symbol:{exchange}:{symbol}", info.ToArray());

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
