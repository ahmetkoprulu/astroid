using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Astroid.Entity;

[Table("Audits")]
public class ADAudit : IEntity
{
	[Key]
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public string Data { get; set; }
	public DateTime CreatedDate { get; set; }
	public DateTime ModifiedDate { get; set; }
}