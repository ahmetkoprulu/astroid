using Microsoft.AspNetCore.Mvc;
using Astroid.Entity;
using Astroid.Web.Models;
using System.Security.Claims;
using Astroid.Core.Cache;
using Microsoft.EntityFrameworkCore;

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

	[HttpGet("profile")]
	public async Task<IActionResult> Profile()
	{
		var user = await Db.Users.FirstOrDefaultAsync(x => x.Id == CurrentUser.Id);
		if (user == null)
			return NotFound("User not found");

		return Success(new AMProfile
		{
			Id = user.Id,
			Name = user.Name,
			Email = user.Email,
			Phone = user.Phone
		});
	}

	[HttpPost("profile")]
	public async Task<IActionResult> SaveProfile([FromBody] AMProfile profile)
	{
		var user = await Db.Users.FirstOrDefaultAsync(x => x.Id == CurrentUser.Id);
		if (user == null)
			return NotFound("User not found");

		user.Name = profile.Name;
		user.Email = profile.Email;
		user.Phone = profile.Phone;

		await Db.SaveChangesAsync();

		return Success(default, "Profile updated successfully");
	}
}