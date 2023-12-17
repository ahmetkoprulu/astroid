using System.Reflection;
using Astroid.Core;
using Astroid.Entity;
using Microsoft.Extensions.DependencyInjection;

namespace Astroid.Providers;

public class ExchangerFactory
{
	public static ExchangeProviderBase? Create(IServiceProvider serviceProvider, ADExchange exchange)
	{
		if (exchange?.Provider == null) return null;

		ExchangeProviderBase? provider;
		switch (exchange.Provider.Name)
		{
			case "binance-usd-futures":
				provider = serviceProvider.GetRequiredService<BinanceUsdFuturesProvider>();
				provider.Context(exchange.PropertiesJson);
				break;
			default:
				return null;
		}

		return provider;
	}

	public static ExchangeProviderBase? Create(IServiceScope scope, ADExchange exchange)
	{
		if (exchange?.Provider == null) return null;

		ExchangeProviderBase? provider;
		switch (exchange.Provider.Name)
		{
			case "binance-usd-futures":
				provider = scope.ServiceProvider.GetRequiredService<BinanceUsdFuturesProvider>();
				provider.Context(exchange.PropertiesJson);
				break;
			default:
				return null;
		}

		return provider;
	}

	public static ExchangeProviderBase? Create(string targetType)
	{
		var assembly = Assembly.GetExecutingAssembly();
		var type = assembly.GetType(targetType);
		if (type == null) return null;

		if (Activator.CreateInstance(type) is not ExchangeProviderBase instance) return null;

		return instance;
	}
}
