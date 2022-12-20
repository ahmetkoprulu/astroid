using System.ComponentModel.DataAnnotations;

namespace Astroid.Web.Models;

public class AMUser
{
	public Guid Id { get; set; }
	public string Name { get; set; }
	public string Email { get; set; }
}