using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Astroid.Core;

namespace Astroid.Entity;

[Table("Audits")]
public class ADAudit : IEntity
{
	[Key]
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public Guid ActorId { get; set; } // ex: bot id
	public Guid? TargetId { get; set; } // ex: position id, order id
	public AuditType Type { get; set; }
	public string Description { get; set; } = string.Empty;
	public string? CorrelationId { get; set; }
	public string? Data { get; set; }
	public DateTime CreatedDate { get; set; }
}
