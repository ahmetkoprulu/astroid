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
	public decimal WeightedEntryPrice { get; set; }
	public decimal Quantity { get; set; }
	public decimal CurrentQuantity { get; set; }
	public int Leverage { get; set; }
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

	public List<ADOrder> Orders { get; set; } = new();

	public void Close()
	{
		Status = PositionStatus.Closed;
		UpdatedDate = DateTime.UtcNow;
	}

	public void Reject()
	{
		Status = PositionStatus.Rejected;
		UpdatedDate = DateTime.UtcNow;
	}

	public void Reduce(decimal quantity)
	{
		CurrentQuantity -= quantity;
		UpdatedDate = DateTime.UtcNow;
	}

	public void Expand(decimal quantity, decimal entryPrice)
	{
		AvgEntryPrice = Status == PositionStatus.Requested ? entryPrice : (AvgEntryPrice + entryPrice) / 2;
		WeightedEntryPrice += entryPrice * quantity;

		if (Status == PositionStatus.Requested)
		{
			Status = PositionStatus.Open;
			EntryPrice = entryPrice;
		}

		Quantity += quantity;
		CurrentQuantity += quantity;
		UpdatedDate = DateTime.UtcNow;
	}

	public void ChangeLeverage(int leverage)
	{
		Leverage = leverage;
		UpdatedDate = DateTime.UtcNow;
	}
}
