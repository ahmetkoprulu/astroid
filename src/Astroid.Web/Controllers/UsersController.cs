using Microsoft.AspNetCore.Mvc;
using Astroid.Entity;
using Astroid.Web.Models;
using System.Security.Claims;
using Astroid.Core.Cache;

namespace Astroid.Web;

public class UsersController : SecureController
{
	public UsersController(AstroidDb db, ICacheService cache) : base(db, cache) { }

	[HttpGet("user-info")]
	public IActionResult GetUserInfo()
	{
		var claims = HttpContext.User.Claims;
		var user = new AMUser
		{
			Id = Guid.Parse(claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value),
			Name = claims.First(x => x.Type == ClaimTypes.Name).Value,
			Email = claims.First(x => x.Type == ClaimTypes.Email).Value
		};

		return Success(user);
	}
}