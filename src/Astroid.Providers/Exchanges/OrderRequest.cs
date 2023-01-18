using Astroid.Core;

namespace Astroid.Providers;

public class OrderRequest
{
	public string Ticker { get; set; }
	public int Leverage { get; set; }
	public OrderType OrderType { get; set; }
	public PositionType PositionType { get; set; }
	public string Timestamp { get; set; }
}