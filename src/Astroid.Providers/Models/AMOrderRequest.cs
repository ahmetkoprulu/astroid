using Astroid.Core;
using Astroid.Entity;
using Newtonsoft.Json;

namespace Astroid.Providers;

public class AMOrderRequest
{
	public Guid? OrderId { get; set; }
	public string Ticker { get; set; } = string.Empty;
	public int? Leverage { get; set; }
	public string Type { get; set; } = string.Empty;
	public int Risk { get; set; } = 1;
	public decimal? Quantity { get; set; }
	public string? QuantityType;
	public string Key { get; set; } = string.Empty;
	public bool IsPyramiding { get; set; }

	public OrderType OrderType => Type switch
	{
		"open-long" => OrderType.Buy,
		"open-short" => OrderType.Sell,
		"close-long" => OrderType.Sell,
		"close-short" => OrderType.Buy,
		"close-all" => OrderType.Sell,
		_ => throw new Exception("Invalid order type")
	};

	public PositionType PositionType => Type switch
	{
		"open-long" => PositionType.Long,
		"open-short" => PositionType.Short,
		"close-long" => PositionType.Long,
		"close-short" => PositionType.Short,
		"close-all" => PositionType.Both,
		_ => throw new Exception("Invalid position type")
	};

	public PositionSizeType QtyType => QuantityType switch
	{
		"percentage" => PositionSizeType.Ratio,
		"fixed-usd" => PositionSizeType.FixedInUsd,
		"fixed-asset" => PositionSizeType.FixedInAsset,
		_ => PositionSizeType.Unknown
	};

	public void SetQuantityType(PositionSizeType type) => QuantityType = type switch
	{
		PositionSizeType.Ratio => QuantityType = "percentage",
		PositionSizeType.FixedInUsd => QuantityType = "fixed-usd",
		PositionSizeType.FixedInAsset => QuantityType = "fixed-asset",
		_ => throw new NotImplementedException(),
	};

	public void SetPositionType(OrderTriggerType triggerType, PositionType positionType) => Type = triggerType switch
	{
		OrderTriggerType.StopLoss => positionType == PositionType.Long ? "close-long" : "close-short",
		OrderTriggerType.TakeProfit => positionType == PositionType.Long ? "close-long" : "close-short",
		OrderTriggerType.Pyramiding => positionType == PositionType.Long ? "open-long" : "open-short",
		OrderTriggerType.Sell => positionType == PositionType.Long ? "close-long" : "close-short",
		OrderTriggerType.Buy => positionType == PositionType.Long ? "open-long" : "open-short",
		_ => throw new InvalidDataException("Invalid order trigger type."),
	};

	public bool IsClose => Type.StartsWith("close");

	public AMOrderRequest? GetSwingRequest()
	{
		if (IsClose) return null;

		return new AMOrderRequest
		{
			Ticker = Ticker,
			Type = Type == "open-long" ? "close-short" : "close-long",
			Key = Key
		};
	}

	public static AMOrderRequest GenerateRequest(ADOrder order)
	{
		var request = new AMOrderRequest
		{
			OrderId = order.Id,
			Ticker = order.Symbol,
			Leverage = order.Position.Leverage,
			Quantity = order.ClosePosition ? 0 : order.Quantity,
			IsPyramiding = order.TriggerType == OrderTriggerType.Pyramiding,
			Key = order.Bot.Key
		};
		request.SetQuantityType(order.QuantityType);
		request.SetPositionType(order.TriggerType, order.Position.Type);

		return request;
	}
}

