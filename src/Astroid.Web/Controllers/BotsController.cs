using Microsoft.AspNetCore.Mvc;
using Astroid.Entity;
using Astroid.Web.Models;
using Astroid.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Astroid.Web;

public class BotsController : BaseController
{
	public BotsController(AstroidDb db) : base(db) { }

	// [HttpPost("{id}")]
	// public async Task<IActionResult> SignUp(AMSignUp model)
	// {
	// }
}