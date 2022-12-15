using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Astroid.Entity;

public class ADOrder : IEntity
{
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public string Pair { get; set; }
	public DateTime CreatedDate { get; set; }
}