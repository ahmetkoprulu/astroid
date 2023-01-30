using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Astroid.Core;

namespace Astroid.Web.Models;

public class AMBot
{
	public Guid Id { get; set; }
	public string Label { get; set; }
	public string? Description { get; set; }
	public Guid ExchangeId { get; set; }
	public OrderEntryType OrderType { get; set; } = OrderEntryType.Market;
	public OrderMode OrderMode { get; set; }
	public PositionSizeType PositionSizeType { get; set; } = PositionSizeType.Ratio;
	public decimal PositionSize { get; set; }
	public bool IsTakePofitEnabled { get; set; }
	public decimal? ProfitActivation { get; set; }
	public bool IsStopLossEnabled { get; set; }
	public decimal? StopLossActivation { get; set; }
	public string Key { get; set; }
	public bool IsEnabled { get; set; }
}