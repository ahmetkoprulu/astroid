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
	public SecureController(AstroidDb db) : base(db)
	{
	}
}