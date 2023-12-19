using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Astroid.Core;

namespace Astroid.Entity;

[Table("Orders")]
public class ADOrder : IEntity
{
	[Key]
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public Guid BotId { get; set; }
	public Guid ExchangeId { get; set; }
	public Guid PositionId { get; set; }
	public string Symbol { get; set; } = string.Empty;
	public int? Leverage { get; set; }
	public OrderTriggerType TriggerType { get; set; }
	public decimal TriggerPrice { get; set; }
	public decimal FilledPrice { get; set; }
	public OrderConditionType ConditionType { get; set; }
	public decimal Quantity { get; set; }
	public PositionSizeType QuantityType { get; set; }
	public decimal FilledQuantity { get; set; }
	public decimal RealizedPnl { get; set; }
	public bool ClosePosition { get; set; }
	public OrderStatus Status { get; set; }
	public Guid? RelatedTo { get; set; }
	public DateTime UpdatedDate { get; set; }
	public DateTime CreatedDate { get; set; }

	[ForeignKey(nameof(PositionId))]
	public ADPosition Position { get; set; } = null!;

	[ForeignKey(nameof(ExchangeId))]
	public ADExchange Exchange { get; set; } = null!;

	[ForeignKey(nameof(BotId))]
	public ADBot Bot { get; set; } = null!;

	[ForeignKey(nameof(UserId))]
	public ADUser User { get; set; } = null!;

	public void Cancel()
	{
		Status = OrderStatus.Cancelled;
		UpdatedDate = DateTime.UtcNow;
	}

	public void Reject()
	{
		if (Position?.Status == PositionStatus.Requested && TriggerType == OrderTriggerType.Buy)
			Position.Reject();

		Status = OrderStatus.Rejected;
		UpdatedDate = DateTime.UtcNow;
	}

	public void Fill(decimal quantity, decimal? price = null)
	{
		if (price.HasValue)
			FilledPrice = price.Value;

		Status = OrderStatus.Filled;
		UpdatedDate = DateTime.UtcNow;
		FilledQuantity = quantity;
	}
}
