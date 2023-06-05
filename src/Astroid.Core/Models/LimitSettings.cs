namespace Astroid.Core;

public class LimitSettings
{
	public ValorizationType ValorizationType { get; set; }
	public decimal Deviation { get; set; }
	public int OrderTimeout { get; set; } = 5;
	public bool ComputeEntryPoint { get; set; }
	public OrderBookComputationMethod ComputationMethod { get; set; } = OrderBookComputationMethod.StandardDeviation;
	public string Code { get; set; } = "// Entries are bids for long and asks for short.\n// Order book depth is 1000.\n// Order book is sorted by price.\n\n";
	public bool ForceUntilFilled { get; set; }
	public int ForceTimeout { get; set; } = 60;
	public int OrderBookSkip { get; set; } = 1;
	public int OrderBookOffset { get; set; } = 3;
}

public enum OrderBookComputationMethod : short
{
	StandardDeviation = 1,
	Code = 2
}
