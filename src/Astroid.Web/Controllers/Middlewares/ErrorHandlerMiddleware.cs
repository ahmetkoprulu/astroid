using System.Net;
using Astroid.Web.Models;
using Microsoft.AspNetCore.Diagnostics;
using Newtonsoft.Json;

namespace Astroid.Web.Middleware;

public class ErrorHandlerMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<ErrorHandlerMiddleware> _logger;

	public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}

	public async Task Invoke(HttpContext context)
	{
		try
		{
			await _next(context);
		}
		catch (Exception error)
		{
			await HandleError(context, error);
		}
	}

	private async Task HandleError(HttpContext context, Exception error)
	{
		var message = error?.Message ?? string.Empty;
		var internalMessage = string.Empty;
		object? data = null;
		var code = 0;

		if (error != null) _logger.LogError(error, "HandleError");

		var response = context.Response;
		response.StatusCode = error switch
		{
			_ => (int)HttpStatusCode.InternalServerError,// unhandled error
		};

		await HandleErrorHtml(context, message, internalMessage, data, code);
	}

	private static async Task HandleErrorAjax(HttpContext context, string message, string internalMessage, object data, int code)
	{
		var response = context.Response;
		response.ContentType = "application/json";
		var result = JsonConvert.SerializeObject(new AMReturn
		{
			Code = code != 0 ? code : response.StatusCode,
			Data = data,
			Message = message,
			InternalMessage = internalMessage,
			Success = false
		});
		await response.WriteAsync(result);
	}

	private static async Task HandleErrorHtml(HttpContext context, string message, string internalMessage, object? data, int code)
	{
		var response = context.Response;
		response.StatusCode = (int)HttpStatusCode.InternalServerError;
		response.ContentType = "text/html";

		await response.WriteAsync("<html lang=\"en\"><body>\r\n");
		await response.WriteAsync("<h2>Astroid Error Handler</h2>\r\n");

		var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();

		await response.WriteAsync("<h4>Information</h4>");
		await response.WriteAsync($"<pre>{message}</pre>");
		await response.WriteAsync($"<h4>Internal Message ({code})</h4>");
		await response.WriteAsync($"<pre>{internalMessage}</pre>");

		var result = JsonConvert.SerializeObject(new AMReturn
		{
			Code = code != 0 ? code : response.StatusCode,
			Data = data,
			Message = message,
			InternalMessage = internalMessage,
			Success = false
		});
		await response.WriteAsync($"<pre>{result}</pre>");

		var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
		if (environment.IsDevelopment())
		{
			await response.WriteAsync("<h4>Additional Details</h4>");
		}

		await response.WriteAsync("<a href=\"/\">Try to return home</a><br>\r\n");
		await response.WriteAsync("</body></html>\r\n");
	}

	public bool IsAjaxRequest(HttpRequest request)
	{
		if (request == null)
			throw new ArgumentNullException(nameof(request));
		if (request.Headers != null) return request.Headers["X-Requested-With"] == "XMLHttpRequest";
		return false;
	}
}

