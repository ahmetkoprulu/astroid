using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Astroid.Entity;
using Astroid.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Astroid.Core;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Astroid.Web;

[Authorize(AuthenticationSchemes = ACWeb.Authentication.DefaultSchema)]
public class SecureController : BaseController
{
	private ADUser _CurrentUser { get; set; }
	public ADUser CurrentUser
	{
		get
		{
			if (_CurrentUser != null)
				return _CurrentUser;

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
				throw new Exception("User not found");

			_CurrentUser = Db.Users.FirstOrDefault(x => x.Id == Guid.Parse(userId));
			if (_CurrentUser == null)
				throw new Exception("Current user not found");

			return _CurrentUser;
		}
	}


	public SecureController(AstroidDb db) : base(db)
	{
	}
}