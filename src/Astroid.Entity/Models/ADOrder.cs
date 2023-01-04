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
	public string Symbol { get; set; }
	public OrderEntryType EntryType { get; set; }
	public decimal EntryPrice { get; set; }

	public DateTime CreatedDate { get; set; }
}