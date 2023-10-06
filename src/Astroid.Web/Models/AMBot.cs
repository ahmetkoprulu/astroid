using Astroid.Core;
using Astroid.Entity;

namespace Astroid.Web.Models;

public class AMBot
{
	public Guid Id { get; set; }
	public string Label { get; set; } = string.Empty;
	public string? Description { get; set; }
	public Guid ExchangeId { get; set; }
	public OrderEntryType OrderType { get; set; } = OrderEntryType.Market;
	public OrderMode OrderMode { get; set; } = OrderMode.TwoWay;
	public PositionSizeType PositionSizeType { get; set; } = PositionSizeType.FixedInUsd;
	public LimitSettings LimitSettings { get; set; } = new();
	public decimal? PositionSize { get; set; } = null;
	public bool IsPositionSizeExpandable { get; set; }
	public bool IsPyramidingEnabled { get; set; }
	public PyramidingSettings PyramidingSettings { get; set; } = new();
	public bool IsTakePofitEnabled { get; set; }
	public TakeProfitSettings TakeProfitSettings { get; set; } = new();
	public bool IsStopLossEnabled { get; set; }
	public StopLossType StopLossType { get; set; } = StopLossType.Fixed;
	public decimal? StopLossPrice { get; set; }
	public decimal? StopLossActivation { get; set; }
	public decimal? StopLossCallbackRate { get; set; }
	public string Key { get; set; } = string.Empty;
	public bool IsEnabled { get; set; }
	public DateTime CreatedDate { get; set; }
}
