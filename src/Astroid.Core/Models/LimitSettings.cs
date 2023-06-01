namespace Astroid.Core;

public class LimitSettings
{
	public ValorizationType ValorizationType { get; set; }
	public decimal Deviation { get; set; }
	public int OrderTimeout { get; set; } = 5;
	public bool ForceUntilFilled { get; set; }
	public int ForceTimeout { get; set; } = 60;
	public int OrderBookSkip { get; set; } = 1;
	public int OrderBookOffset { get; set; } = 3;
}
