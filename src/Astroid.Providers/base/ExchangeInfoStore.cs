using System.Collections.Concurrent;

namespace Astroid.Providers;

public static class ExhangeInfoStore
{
	private static ConcurrentDictionary<string, AMExchangeInfo> ExchangeInfo { get; set; } = new();

	public static AMExchangeInfo GetOrAdd(string exchange, Func<AMExchangeInfo> func)
	{
		var isExist = ExchangeInfo.TryGetValue(exchange, out var info);
		if (isExist && info?.ModifiedAt > DateTime.UtcNow.AddDays(-1)) return info;

		var newInfo = func();
		ExchangeInfo.AddOrUpdate(exchange, newInfo, (key, oldInfo) => newInfo);

		return newInfo;
	}

	public static AMSymbolInfo? GetSymbolInfo(string exchange, string symbol, Func<AMExchangeInfo> func)
	{
		var info = GetOrAdd(exchange, func);
		return info.Symbols.FirstOrDefault(x => x.Name == symbol);
	}
}
