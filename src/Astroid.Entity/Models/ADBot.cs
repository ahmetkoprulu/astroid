using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Astroid.Core;

namespace Astroid.Entity;

[Table("Bots")]
public class ADBot : IEntity
{
	[Key]
	public Guid Id { get; set; }
	public string Label { get; set; }
	public string Description { get; set; } = string.Empty;
	public Guid ExchangeId { get; set; }
	// public SignalSourceType SignalSourceType { get; set; }
	public OrderEntryType OrderType { get; set; }
	public PositionSizeType PositionSizeType { get; set; }
	public decimal PositionSize { get; set; }
	public bool IsTakePofitEnabled { get; set; }
	// public decimal? ProfitSize { get; set; }
	public decimal? ProfitActivation { get; set; }
	public bool IsStopLossActivated { get; set; }
	public decimal? StopLossActivation { get; set; }
	public bool IsEnabled { get; set; }
	public Guid UserId { get; set; }
	public DateTime CreatedDate { get; set; }
	public DateTime ModifiedDate { get; set; }
}
