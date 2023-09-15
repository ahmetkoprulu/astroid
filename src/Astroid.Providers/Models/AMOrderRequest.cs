using Astroid.Core;
using Astroid.Entity;
using Newtonsoft.Json;

namespace Astroid.Providers;

public class AMOrderRequest
{
	public Guid? OrderId { get; set; }
	public string Ticker { get; set; } = string.Empty;
	public int Leverage { get; set; }
	public string Type { get; set; } = string.Empty;
	public int Risk { get; set; } = 1;
	public decimal? Quantity { get; set; }
	public QuantityType QuantityType { get; set; } = QuantityType.Percentage;
	public string Key { get; set; } = string.Empty;

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

	public string? Timestamp { get; set; }

	public bool ValidateOpenRequest(ADPosition? position, ADBot bot, AMProviderResult result)
	{
		if (position != null && position.BotId != bot.Id)
		{
			result.AddAudit(AuditType.OpenOrderPlaced, $"The position for {Ticker} - {position.Type} already exists and managed by {position.Bot.Label}.", data: JsonConvert.SerializeObject(new { Ticker, OrderType, PositionType }));
			return false;
		}

		if (position != null && !bot.IsPositionSizeExpandable)
		{
			result.AddAudit(AuditType.OpenOrderPlaced, $"Position size is not expandable", data: JsonConvert.SerializeObject(new { Ticker, OrderType, PositionType }));
			return false;
		}

		if (bot.OrderMode == OrderMode.OneWay && position != null)
		{
			result.AddAudit(AuditType.OpenOrderPlaced, $"Position already exists for {Ticker} - {position.Type}", data: JsonConvert.SerializeObject(new { Ticker, OrderType, PositionType }));
			return false;
		}

		return true;
	}

	public bool ValidateCloseRequest(ADPosition position, ADOrder? order, ADBot bot, AMProviderResult result)
	{
		if (OrderId.HasValue && order == null)
		{
			result.WithMessage("The order not found").AddAudit(AuditType.CloseOrderPlaced, $"The order not found", data: JsonConvert.SerializeObject(new { OrderId, Ticker, OrderType, PositionType }));
			return false;
		}

		if (position.BotId != bot.Id)
		{
			result.AddAudit(AuditType.OpenOrderPlaced, $"The position for {Ticker} - {position.Type} managed by {position.Bot.Label}.", data: JsonConvert.SerializeObject(new { Ticker, OrderType, PositionType }));
			return false;
		}

		return true;
	}

	public AMOrderRequest? GetSwingRequest()
	{
		if (Type.StartsWith("close")) return null;

		return new AMOrderRequest
		{
			Ticker = Ticker,
			Type = Type == "open-long" ? "close-short" : "close-long",
			Key = Key
		};
	}
}

public enum QuantityType : short
{
	Unknown = 0,
	Percentage = 1,
	Exact = 2
}
