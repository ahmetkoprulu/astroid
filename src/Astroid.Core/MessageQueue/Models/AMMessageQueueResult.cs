namespace Astroid.Core.MessageQueue;

public class AMMessageQueueResult
{
	public bool Success { get; set; }
	public string? Message { get; set; }

	public AMMessageQueueResult(bool success, string? message = null)
	{
		Success = success;
		Message = message;
	}
}
