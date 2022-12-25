using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Astroid.Entity;

[Table("Orders")]
public class ADOrder : IEntity
{
	[Key]
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public string Pair { get; set; }
	public DateTime CreatedDate { get; set; }
}