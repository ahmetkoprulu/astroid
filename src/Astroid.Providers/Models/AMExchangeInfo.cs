namespace Astroid.Providers;

public class AMExchangeInfo
{
	public string Name { get; set; }
	public List<AMSymbolInfo> Symbols { get; set; } = new();
	public DateTime ModifiedAt { get; set; }
}
