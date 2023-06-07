namespace Astroid.Core;

public class LimitSettings
{
	public ValorizationType ValorizationType { get; set; }
	public decimal Deviation { get; set; }
	public int OrderTimeout { get; set; } = 5;
	public bool ComputeEntryPoint { get; set; }
	public OrderBookComputationMethod ComputationMethod { get; set; } = OrderBookComputationMethod.StandardDeviation;
	public int OrderBookDepth { get; set; } = 1000;
	public string Code { get; set; } = "// Entries are asks for long and bids for short.\n// Entries are sorted by ascending for asks and descending for bids.\n// Entry object consist of Price and Quantity properties.\n// Referenced namespaces:\n// using System;\n// using System.Collections.Generic;\n// using System.Linq;\n\n";
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
