using Astroid.Core;

public class AMExchangePosition
{
	public string Symbol { get; set; } = string.Empty;
	public decimal EntryPrice { get; set; }
	public decimal Quantity { get; set; }
	public decimal Leverage { get; set; }
	public PositionType Type { get; set; }
}
