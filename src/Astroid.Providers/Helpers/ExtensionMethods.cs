using System.Reflection;
using Astroid.Core;
using Astroid.Entity;
using Astroid.Providers;
using Binance.Net.Enums;
using CryptoExchange.Net.CommonObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Astroid.Providers.Extentions;

public static class ExtensionMethods
{
	// public static ExchangeProviderBase? GetExchangeProviderBase(this ADExchange exchange, IServiceProvider serviceProvider)
	// {
	// 	if (exchange.Provider == null) return null;

	// 	var generatedType = Type.GetType(exchange.Provider.TargetType) ?? throw new TypeLoadException($"User provider [{exchange.Provider.Name} - {exchange.Provider.TargetType}] type not found.");
	// 	var providerBase = (ExchangeProviderBase)ActivatorUtilities.CreateInstance(serviceProvider, generatedType);
	// 	providerBase.Context(exchange);

	// 	return providerBase;
	// }

	public static List<ProviderPropertyValue> GeneratePropertyMetadata(this ADExchangeProvider provider)
	{
		var generatedType = Type.GetType(provider.TargetType) ?? throw new TypeLoadException($"User provider [{provider.Name} - {provider.TargetType}] type not found.");
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
				Encrypted = metadata.Encrypted,
				Required = metadata.Required,
				DefaultValue = metadata.DefaultValue,
				Type = metadata.Type,
				Data = metadata.Data,
				Order = metadata.Order
			});
		}

		return list;
	}




	#region Enum Conversions

	public static PositionType Reverse(this PositionType type) =>
		type switch
		{
			PositionType.Long => PositionType.Short,
			PositionType.Short => PositionType.Long,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};

	public static PositionType ToAstroid(this PositionSide type) =>
	type switch
	{
		PositionSide.Long => PositionType.Long,
		PositionSide.Short => PositionType.Short,
		_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
	};

	public static PositionSide ToBinance(this PositionType type) =>
		type switch
		{
			PositionType.Long => PositionSide.Long,
			PositionType.Short => PositionSide.Short,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};

	public static OrderType ToAstroid(this OrderSide type) =>
		type switch
		{
			OrderSide.Buy => OrderType.Buy,
			OrderSide.Sell => OrderType.Sell,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};

	public static OrderSide ToBinance(this OrderType type, PositionType pType)
	{
		if (type == OrderType.Buy && pType == PositionType.Long) return OrderSide.Buy;
		else if (type == OrderType.Buy && pType == PositionType.Short) return OrderSide.Sell;
		else if (type == OrderType.Sell && pType == PositionType.Long) return OrderSide.Sell;
		else if (type == OrderType.Sell && pType == PositionType.Short) return OrderSide.Buy;
		else throw new ArgumentOutOfRangeException(nameof(type), type, null);
	}

	#endregion
}
