using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Astroid.Entity;

[Table("Users")]

public class ADUser : IEntity
{
	[Key]
	public Guid Id { get; set; }
	public string Name { get; set; }
	public string Email { get; set; }
	public string PasswordHash { get; set; }
	public DateTime CreatedDate { get; set; }
	public bool IsRemoved { get; set; }
}