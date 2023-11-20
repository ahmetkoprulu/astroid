namespace Astroid.Core.MessageQueue;

public class AQPriceChangeMessage
{
	public Guid ExchangeId { get; set; }
	public string ExchangeName { get; set; } = string.Empty;
	public Dictionary<string, decimal> Prices { get; set; } = new();
}
