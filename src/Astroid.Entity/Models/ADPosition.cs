using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Astroid.Core;

namespace Astroid.Entity;

[Table("Positions")]
public class ADPosition : IEntity
{
	[Key]
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public Guid BotId { get; set; }
	public Guid ExchangeId { get; set; }
	public string Symbol { get; set; } = string.Empty;
	public decimal EntryPrice { get; set; }
	public decimal AvgEntryPrice { get; set; }
	public decimal Quantity { get; set; }
	public PositionType Type { get; set; }
	public PositionStatus Status { get; set; }
	public DateTime UpdatedDate { get; set; }
	public DateTime CreatedDate { get; set; }

	[ForeignKey(nameof(ExchangeId))]
	public ADExchange Exchange { get; set; } = null!;

	[ForeignKey(nameof(BotId))]
	public ADBot Bot { get; set; } = null!;

	[ForeignKey(nameof(UserId))]
	public ADUser User { get; set; } = null!;
}
