using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Astroid.Entity;
using Astroid.Web.Models;

namespace MonoSign.Web.Management
{
	public class BaseController : Controller
	{
		protected AstroidDb Db { get; set; }
		// private readonly ILogger<BaseController> _logger = ALogger.Create<BaseController>();

		public BaseController(AstroidDb db) => Db = db;

		public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			await base.OnActionExecutionAsync(context, next);
		}

		#region Return Types

		[NonAction]
		protected IActionResult Success(object data) => StatusCode(200, data);

		[NonAction]
		protected IActionResult Success(AMReturn data) => StatusCode(200, data);

		[NonAction]
		protected IActionResult Created(object data) => StatusCode(201, data);

		[NonAction]
		protected IActionResult Created(AMReturn data) => StatusCode(201, data);

		[NonAction]
		protected IActionResult BadRequest(AMReturn data) => StatusCode(400, data);

		[NonAction]
		protected IActionResult Unauthorized(AMReturn data) => StatusCode(401, data);

		[NonAction]
		protected IActionResult Forbidden(object data) => StatusCode(403, data);

		[NonAction]
		protected IActionResult Forbidden(AMReturn data) => StatusCode(403, data);

		[NonAction]
		protected IActionResult NotFound(AMReturn data) => StatusCode(404, data);

		[NonAction]
		protected IActionResult Error(object data) => StatusCode(500, data);

		[NonAction]
		protected IActionResult Error(AMReturn data) => StatusCode(500, data);

		#endregion
	}
}
