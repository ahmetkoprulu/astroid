using System.Collections.Concurrent;
using System.Linq;

namespace Astroid.Providers;

public static class ExchangeInfoStore
{
	private static ConcurrentDictionary<string, AMExchangeInfo> ExchangeInfo { get; set; } = new();

	public static AMExchangeInfo GetOrAdd(string exchange, Func<AMExchangeInfo> func)
	{
		var isExist = ExchangeInfo.TryGetValue(exchange, out var info);
		if (isExist && info!.ModifiedAt > DateTime.UtcNow.AddDays(-1)) return info;

		var newInfo = func();
		ExchangeInfo.AddOrUpdate(exchange, newInfo, (key, oldInfo) => newInfo);

		return newInfo;
	}

	public static void Add(string exchange, AMExchangeInfo info) => ExchangeInfo.AddOrUpdate(exchange, info, (key, oldInfo) => info);

	public static AMExchangeInfo? Get(string key)
	{
		_ = ExchangeInfo.TryGetValue(key, out var info);

		return info;
	}

	public static AMSymbolInfo? GetSymbolInfo(string exchange, string symbol, Func<AMExchangeInfo> func)
	{
		var info = GetOrAdd(exchange, func);
		return info.Symbols.FirstOrDefault(x => x.Name == symbol);
	}

	public static AMSymbolInfo? GetSymbolInfo(string exchange, string symbol)
	{
		var info = Get(exchange);
		return info?.Symbols.FirstOrDefault(x => x.Name == symbol);
	}

	public static List<AMExchangeInfo> GetAll() => ExchangeInfo.Values.ToList();

	public static void Clear() => ExchangeInfo.Clear();
}
