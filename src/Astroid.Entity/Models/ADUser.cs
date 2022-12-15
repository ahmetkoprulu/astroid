namespace Astroid.Entity;

public class ADUser : IEntity
{
	public Guid Id { get; set; }
	public string Name { get; set; }
	public string Email { get; set; }
	public string PasswordHash { get; set; }
	public DateTime CreatedDate { get; set; }
	public bool IsRemoved { get; set; }
}