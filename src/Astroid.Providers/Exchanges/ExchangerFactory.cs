using System.Reflection;
using Astroid.Core;
using Astroid.Entity;

namespace Astroid.Providers;

public class ExchangerFactory
{
	public static ExchangeProviderBase? Create(ADExchange exchange)
	{
		var assembly = Assembly.GetExecutingAssembly();
		var type = assembly.GetType(exchange.Provider.TargetType);
		if (type == null) return null;

		if (Activator.CreateInstance(type) is not ExchangeProviderBase instance) return null;

		instance.Context(exchange.Settings);
		return instance;
	}

	public static ExchangeProviderBase? Create(string targetType, string settings)
	{
		var assembly = Assembly.GetExecutingAssembly();
		var type = assembly.GetType(targetType);
		if (type == null) return null;

		if (Activator.CreateInstance(type) is not ExchangeProviderBase instance) return null;

		instance.Context(settings);
		return instance;
	}
}
