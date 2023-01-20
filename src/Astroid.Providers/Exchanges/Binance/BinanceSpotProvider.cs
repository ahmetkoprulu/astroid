using Astroid.Core;
using Astroid.Entity;

namespace Astroid.Providers;

public class BinanceSpotProvider : ExchangeProviderBase
{
	[PropertyMetadata("API Key", Type = PropertyTypes.Text, IsEncrypted = true, Group = "General")]
	public string Key { get; set; } = string.Empty;

	[PropertyMetadata("API Secret", Type = PropertyTypes.Text, Required = true, IsEncrypted = true, Group = "General")]
	public string Secret { get; set; } = string.Empty;

	[PropertyMetadata("Test Net", Type = PropertyTypes.Boolean, Group = "General")]
	public bool IsTestNet { get; set; }

	public override void Context(string settings, ADExchange exchange)
	{
		throw new NotImplementedException();
	}

	public override Task ExecuteOrder(ADBot bot, OrderRequest order)
	{
		throw new NotImplementedException();
	}

	public override void Dispose()
	{
		throw new NotImplementedException();
	}
}
