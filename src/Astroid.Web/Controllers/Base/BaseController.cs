using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Astroid.Entity;
using Astroid.Web.Models;
using System.Security.Claims;
using Astroid.Core;
using Microsoft.AspNetCore.Authentication;

namespace Astroid.Web;

[ApiController]
[Route("api/[controller]")]
public class BaseController : Controller
{
	protected AstroidDb Db { get; set; }
	// private readonly ILogger<BaseController> _logger = ALogger.Create<BaseController>();

	public BaseController(AstroidDb db) => Db = db;

	public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
	{
		await base.OnActionExecutionAsync(context, next);
	}

	[NonAction]
	public async Task AuthenticateUser(ADUser user, Guid sessionId, bool isPermanent)
	{
		var claims = new List<Claim>
			{
				new Claim(ACWeb.Authentication.SessionKey, sessionId.ToString()),
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Name, user.Name),
				new Claim(ClaimTypes.Email, user.Email),
			};

		var claimsIdentity = new ClaimsIdentity(claims, ACWeb.Authentication.DefaultSchema);
		var principle = new ClaimsPrincipal(claimsIdentity);

		await HttpContext.SignOutAsync(ACWeb.Authentication.DefaultSchema);
		var authProperties = new AuthenticationProperties
		{
			IsPersistent = isPermanent,
			ExpiresUtc = DateTime.UtcNow.AddDays(30),
			AllowRefresh = true
		};
		await HttpContext.SignInAsync(ACWeb.Authentication.DefaultSchema, principle, authProperties);
	}

	#region Return Types

	[NonAction]
	protected IActionResult Success(object data, string message = "") => StatusCode(200, AMReturn.Ok(data, message));

	[NonAction]
	protected IActionResult Success(AMReturn data) => StatusCode(200, data);

	[NonAction]
	protected IActionResult Created(object data, string message = "") => StatusCode(201, AMReturn.Ok(data, message, 201));

	[NonAction]
	protected IActionResult Created(AMReturn data) => StatusCode(201, data);

	[NonAction]
	protected IActionResult BadRequest(AMReturn data) => StatusCode(400, data);

	[NonAction]
	protected IActionResult BadRequest(string message) => StatusCode(400, AMReturn.Bad(message));

	[NonAction]
	protected IActionResult Unauthorized(AMReturn data) => StatusCode(401, data);

	[NonAction]
	protected IActionResult Forbidden(object data) => StatusCode(403, data);

	[NonAction]
	protected IActionResult Forbidden(AMReturn data) => StatusCode(403, data);

	[NonAction]
	protected IActionResult NotFound(AMReturn data) => StatusCode(404, data);

	[NonAction]
	protected IActionResult NotFound(string message) => StatusCode(404, AMReturn.NotFound(message));

	[NonAction]
	protected IActionResult Error(object data) => StatusCode(500, data);

	[NonAction]
	protected IActionResult Error(AMReturn data) => StatusCode(500, data);

	#endregion
}

