using Astroid.Core;
using Astroid.Entity;

namespace Astroid.Web.Models
{
	public class AMPosition
	{
		public Guid Id { get; set; }
		public string Symbol { get; set; } = string.Empty;
		public decimal EntryPrice { get; set; }
		public decimal AveragePrice { get; set; }
		public decimal WeightedEntryPrice { get; set; }
		public decimal Quantity { get; set; }
		public decimal CurrentQuantity { get; set; }
		public decimal Leverage { get; set; }
		public decimal RealizedPnl { get; set; }
		public PositionType Type { get; set; }
		public PositionStatus Status { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime UpdatedDate { get; set; }
		public string BotLabel { get; set; } = string.Empty;
		public AMExchange Exchange { get; set; } = new();
		public List<ADOrder> Orders { get; set; } = new();
	}
}
