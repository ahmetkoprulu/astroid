using Astroid.Core;
using Astroid.Entity;

namespace Astroid.Web.Models
{
	public class AMPosition
	{
		public Guid Id { get; set; }
		public string Symbol { get; set; } = string.Empty;
		public decimal AveragePrice { get; set; }
		public decimal EntryPrice { get; set; }
		public decimal Quantity { get; set; }
		public decimal CurrentQuantity { get; set; }
		public decimal Leverage { get; set; }
		public decimal RealizedPnl { get; set; }
		public PositionType Type { get; set; }
		public PositionStatus Status { get; set; }
		public DateTime CreatedDate { get; set; }
		public List<ADOrder> Orders { get; set; } = new();
		public string BotLabel { get; set; } = string.Empty;
		public string ExchangeLabel { get; set; } = string.Empty;
		public string ExchangeProviderName { get; set; } = string.Empty;
	}
}
