namespace Astroid.Providers;

public class AMExchangeInfo
{
	public List<AMSymbolInfo> Symbols { get; set; } = new();
	public DateTime ModifiedAt { get; set; }
}
