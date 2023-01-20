using System.Reflection;
using Astroid.Core;
using Astroid.Entity;
using Astroid.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Astroid.Providers.Extentions;

public static class ExtensionMethods
{
	public static ExchangeProviderBase? GetExchangeProviderBase(this ADExchange exchange, IServiceProvider serviceProvider)
	{
		if (exchange.Provider == null) return null;

		var generatedType = Type.GetType(exchange.Provider.TargetType);


		if (generatedType == null) throw new TypeLoadException($"User provider [{exchange.Provider.Name} - {exchange.Provider.TargetType}] type not found.");

		var providerBase = (ExchangeProviderBase)ActivatorUtilities.CreateInstance(serviceProvider, generatedType);
		providerBase.Context(exchange.PropertiesJson, exchange);

		return providerBase;
	}

	public static List<ProviderPropertyValue> GeneratePropertyMetadata(this ADExchangeProvider provider)
	{
		var generatedType = Type.GetType(provider.TargetType);
		if (generatedType == null) throw new TypeLoadException($"User provider [{provider.Name} - {provider.TargetType}] type not found.");

		var list = new List<ProviderPropertyValue>();
		var propInfos = generatedType.GetProperties();
		foreach (var prop in propInfos)
		{
			var metadata = prop.GetCustomAttribute<PropertyMetadataAttribute>();
			if (metadata == null) continue;

			list.Add(new ProviderPropertyValue
			{
				Property = prop.Name,
				DisplayName = metadata.DisplayName,
				Description = metadata.Description,
				Required = metadata.Required,
				DefaultValue = metadata.DefaultValue,
				Type = metadata.Type,
				Data = metadata.Data,
				Order = metadata.Order
			});
		}

		return list;
	}
}