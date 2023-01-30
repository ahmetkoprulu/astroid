using Astroid.Core;

namespace Astroid.Providers;

public class AMOrderRequest
{
	public string Ticker { get; set; }
	public int Leverage { get; set; }
	public string? Type { get; set; }
	public OrderType OrderType
	{
		get
		{
			return Type switch
			{
				"open-long" => OrderType.Buy,
				"open-short" => OrderType.Sell,
				"close-long" => OrderType.Sell,
				"close-short" => OrderType.Buy,
				_ => throw new Exception("Invalid order type")
			};
		}
	}
	public PositionType PositionType
	{
		get
		{
			return Type switch
			{
				"open-long" => PositionType.Long,
				"open-short" => PositionType.Short,
				"close-long" => PositionType.Long,
				"close-short" => PositionType.Short,
				_ => throw new Exception("Invalid position type")
			};
		}
	}
	public string? Timestamp { get; set; }
}