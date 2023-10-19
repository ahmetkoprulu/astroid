using Astroid.Core.Cache;

namespace Astroid.Providers;

public class AMSymbolInfo
{
	public string SymbolKey => $"Symbol:{Exchange}:{Name}";
	public ICacheService Cache { get; set; }
	public string Exchange { get; set; }
	public string Name { get; set; }

	public string BaseAsset { get; set; }
	public async Task<string?> GetBaseAsset() => await Cache.GetHash<string>(SymbolKey, "BaseAsset");
	public async Task SetBaseAsset(string value) => await Cache.SetHash(SymbolKey, "BaseAsset", value);

	public string QuoteAsset { get; set; }
	public async Task<string?> GetQuoteAsset() => await Cache.GetHash<string>(SymbolKey, "QuoteAsset");
	public async Task SetQuoteAsset(string value) => await Cache.SetHash(SymbolKey, "QuoteAsset", value);

	public int QuantityPrecision { get; set; }
	public async Task<int> GetQuantityPrecision() => await Cache.GetHash<int>(SymbolKey, "QuantityPrecision");
	public async Task SetQuantityPrecision(int value) => await Cache.SetHash(SymbolKey, "QuantityPrecision", value);

	public int PricePrecision { get; set; }
	public async Task<int> GetPricePrecision() => await Cache.GetHash<int>(SymbolKey, "PricePrecision");
	public async Task SetPricePrecision(int value) => await Cache.SetHash(SymbolKey, "PricePrecision", value);

	public decimal LastPrice { get; set; }
	public async Task<decimal> GetLastPrice() => await Cache.GetHash<decimal>(SymbolKey, "LastPrice");
	public async Task SetLastPrice(decimal value) => await Cache.SetHash(SymbolKey, "LastPrice", value);

	public decimal MarkPrice { get; set; }
	public async Task<decimal> GetMarkPrice() => await Cache.GetHash<decimal>(SymbolKey, "MarkPrice");
	public async Task SetMarkPrice(decimal value) => await Cache.SetHash(SymbolKey, "MarkPrice", value);

	public AMSymbolInfo() { }

	public AMSymbolInfo(string exchange, string name, ICacheService cache)
	{
		Exchange = exchange;
		Name = name;
		Cache = cache;
	}

	public AMOrderBook GetOrderBook() => new(Exchange, Name, Cache);
}
