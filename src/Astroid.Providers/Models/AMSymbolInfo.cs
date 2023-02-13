namespace Astroid.Providers;

public class AMSymbolInfo
{
	public string Name { get; set; } = string.Empty;
	public decimal? TickSize { get; set; }
	public int QuantityPrecision { get; set; }
	public int PricePrecision { get; set; }
}