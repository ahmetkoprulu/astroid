using System.Net;
using Newtonsoft.Json;

namespace Astroid.Web.Models
{
	public class AMReturn<T>
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public T Data { get; set; }
		public bool Success { get; set; }
		public string Message { get; set; }
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string InternalMessage { get; set; }
		public int Code { get; set; }

		public static AMReturn<T> Error(string internalMessage, string message = null, int code = 500, T data = default) => new AMReturn<T>
		{
			Message = message,
			Code = code,
			InternalMessage = internalMessage,
			Success = false,
			Data = data,
		};

		public static AMReturn<T> NotFound(string internalMessage = null, string message = null, int code = 404, T data = default) => new AMReturn<T>
		{
			Message = message,
			Code = code,
			InternalMessage = internalMessage ?? "Resource not found.",
			Success = false,
			Data = data,
		};

		public static AMReturn<T> NeedMoreInformation(string internalMessage, string message = null, T data = default) => new AMReturn<T>
		{
			Message = message,
			Code = (int)HttpStatusCode.UnprocessableEntity,
			InternalMessage = internalMessage,
			Success = false,
			Data = data,
		};

		public static AMReturn<T> Bad(string internalMessage, string message = null, int code = 400, T data = default) => new AMReturn<T>
		{
			Message = message,
			Code = code,
			InternalMessage = internalMessage,
			Success = false,
			Data = data,
		};

		public static AMReturn<T> Ok(T data = default, string internalMessage = null, string message = null, int code = 200) => new AMReturn<T>
		{
			Message = message,
			InternalMessage = internalMessage,
			Code = code,
			Success = true,
			Data = data
		};
	}

	public class AMReturn : AMReturn<object>
	{
	}
}