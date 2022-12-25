using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Astroid.Entity;

[Table("Exchanges")]
public class ADExchange : IEntity
{
	[Key]
	public Guid Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public string Settings { get; set; }
	public Guid ProviderId { get; set; }

	[ForeignKey(nameof(ProviderId))]
	public ADExchangeProvider Provider { get; set; }
	public Guid UserId { get; set; }
	public DateTime CreatedDate { get; set; }
	public DateTime ModifiedDate { get; set; }
}
