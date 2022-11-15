using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Astroid.Web.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Astroid.Web.Middleware
{
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
			var message = error?.Message;
			var internalMessage = default(string);
			var data = default(object);
			var code = 0;

			if (error != null) _logger.LogError(error, "HandleError");

			var response = context.Response;
			switch (error)
			{
				default:
					// unhandled error
					response.StatusCode = (int)HttpStatusCode.InternalServerError;
					break;
			}
			if (IsAjaxRequest(context.Request))
			{
				await HandleErrorAjax(context, message, internalMessage, data, code);
			}
			else
			{
				await HandleErrorHtml(context, message, internalMessage, data, code);
			}
		}

		private static async Task HandleErrorAjax(HttpContext context, string message, string internalMessage, object data, int code)
		{
			var response = context.Response;
			response.ContentType = "application/json";
			var result = JsonSerializer.Serialize(new AMReturn
			{
				Code = code != 0 ? code : response.StatusCode,
				Data = data,
				Message = message,
				InternalMessage = internalMessage,
				Success = false
			});
			await response.WriteAsync(result);
		}

		private static async Task HandleErrorHtml(HttpContext context, string message, string internalMessage, object data, int code)
		{
			var response = context.Response;
			response.StatusCode = (int)HttpStatusCode.InternalServerError;
			response.ContentType = "text/html";

			await response.WriteAsync("<html lang=\"en\"><body>\r\n");
			await response.WriteAsync("<h2>MonoSign Handled Exception</h2>\r\n");

			var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();

			await response.WriteAsync("<h4>Information</h4>");
			await response.WriteAsync($"<pre>{message}</pre>");
			await response.WriteAsync($"<h4>Internal Message ({code})</h4>");
			await response.WriteAsync($"<pre>{internalMessage}</pre>");

			var result = JsonSerializer.Serialize(new AMReturn
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
}
