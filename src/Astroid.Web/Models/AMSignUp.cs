using System.ComponentModel.DataAnnotations;

namespace Astroid.Web.Models;

public class AMSignUp
{
	public string Name { get; set; }
	public string Email { get; set; }
	public string Password { get; set; }
	public string ConfirmPassword { get; set; }
}