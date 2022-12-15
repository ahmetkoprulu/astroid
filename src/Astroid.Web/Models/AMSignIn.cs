using System.ComponentModel.DataAnnotations;

namespace Astroid.Web.Models;

public class AMSignIn
{
	[Required(ErrorMessage = "Email is required")]
	[EmailAddress(ErrorMessage = "Invalid email format")]
	public string Email { get; set; }

	[Required(ErrorMessage = "Password is required")]
	[MinLength(8, ErrorMessage = "Password should have at least 8 characters")]
	public string Password { get; set; }

	public bool RememberMe { get; set; }
}