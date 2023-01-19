using System.Reflection;
using Astroid.Core;
using Astroid.Entity;

namespace Astroid.Providers;

public class ExchangerFactory
{
	public static ExchangeProviderBase? Create(ADExchange exchange)
	{
		if (exchange?.Provider == null) return null;

		ExchangeProviderBase? provider;
		switch (exchange.Provider.Name)
		{
			case "binance-usd-futures":
				provider = new BinanceUsdFuturesProvider();
				break;
			default:
				return null;
		}

		provider.Context(exchange.PropertiesJson, exchange);

		return provider;
	}

	public static ExchangeProviderBase? Create(string targetType, ADExchange exchange)
	{
		var assembly = Assembly.GetExecutingAssembly();
		var type = assembly.GetType(targetType);
		if (type == null) return null;

		if (Activator.CreateInstance(type) is not ExchangeProviderBase instance) return null;

		instance.Context(exchange.PropertiesJson, exchange);

		return instance;
	}
}
