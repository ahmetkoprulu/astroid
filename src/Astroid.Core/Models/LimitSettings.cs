namespace Astroid.Core;

public class LimitSettings
{
	public ValorizationType ValorizationType { get; set; }
	public decimal Deviation { get; set; }
	public int OrderTimeout { get; set; } = 5;
	public int OrderBookOffset { get; set; } = 3;
}