namespace Astroid.Providers;

public class AMSymbolInfo
{
	public string Name { get; set; } = string.Empty;
	public decimal? TickSize { get; set; }
	public int QuantityPrecision { get; set; }
	public int PricePrecision { get; set; }
	public decimal LastPrice { get; set; }
	public decimal MarkPrice { get; set; }
	public DateTime ModifiedAt { get; set; }
	public AMOrderBook OrderBook { get; set; }

	public void SetLastPrice(decimal price)
	{
		LastPrice = price;
		ModifiedAt = DateTime.UtcNow;
	}

	public void SetMarkPrice(decimal price)
	{
		MarkPrice = price;
		ModifiedAt = DateTime.UtcNow;
	}
}
