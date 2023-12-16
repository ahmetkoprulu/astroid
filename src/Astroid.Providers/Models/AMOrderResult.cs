namespace Astroid.Providers;

public class AMOrderResult
{
	public bool Success { get; set; }
	public string? Message { get; set; }
	public string? ClientOrderId { get; set; }
	public decimal EntryPrice { get; set; }
	public decimal Quantity { get; set; }

	public static AMOrderResult WithSuccess() => new() { Success = true };

	public static AMOrderResult WithSuccess(decimal entryPrice, decimal quantity, string? clientOrderId = null)
		=> new() { Success = true, ClientOrderId = clientOrderId, EntryPrice = entryPrice, Quantity = quantity };

	public static AMOrderResult WithFailure(string? message) => new() { Success = false, Message = message };
}
