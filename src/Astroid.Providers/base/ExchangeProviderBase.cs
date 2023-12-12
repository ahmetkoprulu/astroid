using System.ComponentModel;
using System.Reflection;
using Astroid.Core;
using Astroid.Entity;
using Astroid.Entity.Extentions;
using CryptoExchange.Net.CommonObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Astroid.Providers;

public abstract class ExchangeProviderBase : IDisposable
{
	protected ExchangeInfoStore ExchangeStore { get; set; }
	protected ExchangeCalculator Calculator { get; set; }
	public string CorrelationId { get; set; }

	protected ExchangeProviderBase(ExchangeInfoStore infoStore, ExchangeCalculator calculator)
	{
		ExchangeStore = infoStore;
		Calculator = calculator;
		CorrelationId = GenerateCorrelationId();
	}

	public virtual void Context(string properties)
	{
		var propertyValues = JsonConvert.DeserializeObject<List<ProviderPropertyValue>>(properties);
		BindProperties(propertyValues);
	}

	public void BindProperties(List<ProviderPropertyValue> propertyValues)
	{
		if (propertyValues == null) throw new ArgumentException("There is no property to bind this provider.");

		var provider = this;
		var type = GetType();
		var properties = type.GetProperties().Where(x => x.GetCustomAttribute<PropertyMetadataAttribute>() != null).ToList();

		foreach (var property in properties)
		{
			var propertyValue = propertyValues.SingleOrDefault(p => p.Property == property.Name);
			if (propertyValue?.Value == null) continue;

			if (property.PropertyType == propertyValue.Value.GetType())
			{
				property.SetValue(provider, propertyValue.Value);
				continue;
			}

			try
			{
				if (property.PropertyType.IsGenericType)
				{
					var propertyValueString = $"{propertyValue.Value}";
					if (!string.IsNullOrWhiteSpace(propertyValueString))
					{
						property.SetValue(provider,
							JsonConvert.DeserializeObject(propertyValueString, property.PropertyType));
					}

					continue;
				}

				var converter = TypeDescriptor.GetConverter(property.PropertyType);
				var objValue = converter.ConvertFrom(propertyValue.Value.ToString());

				property.SetValue(provider, objValue);
				continue;
			}
			catch
			{
				// Unable to convert basic value
			}

			try
			{
				var value = JsonConvert.DeserializeObject(propertyValue.Value.ToString(), property.PropertyType);
				property.SetValue(provider, value);
			}
			catch
			{
				// Unable to convert complex value
			}
		}
	}

	public static string GenerateCorrelationId()
	{
		var random = new Random();
		var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
		return new string(Enumerable.Repeat(chars, 15)
			.Select(s => s[random.Next(s.Length)]).ToArray());
	}

	public async Task<int> GetEntryPointIndex(AMOrderBook orderBook, PositionType pSide, LimitSettings settings)
	{
		var entryPoint = await GetEntryPoint(orderBook, pSide, settings);
		var i = pSide == PositionType.Long ? await orderBook.GetGreatestAskPriceLessThan(entryPoint) : await orderBook.GetLeastBidPriceGreaterThan(entryPoint);

		if (i <= 0) throw new Exception("Could not find entry point out of order book.");

		return i;
	}

	public async Task<decimal> GetEntryPoint(AMOrderBook orderBook, PositionType pSide, LimitSettings settings)
	{
		if (settings.ComputationMethod == OrderBookComputationMethod.Code)
		{
			var pairs = pSide == PositionType.Long ? await orderBook.GetAsks(settings.OrderBookDepth) : await orderBook.GetBids(settings.OrderBookDepth);
			var entries = pairs.Select(x => new AMOrderBookEntry { Price = x.Key, Quantity = x.Value }).ToList();

			var result = CodeExecutor.ExecuteComputationMethod(settings.Code, entries);
			if (!result.IsSuccess) throw new Exception(result.Message);

			return result.Data;
		}

		var prices = pSide == PositionType.Long ? await orderBook.GetAskPrices(settings.OrderBookDepth) : await orderBook.GetBidPrices(settings.OrderBookDepth);

		var sDeviation = Calculator.CalculateStandardDeviation(prices);
		var mean = prices.Average();

		return pSide == PositionType.Long ? mean + (2 * sDeviation) : mean - (2 * sDeviation);
	}

	protected async Task<AMSymbolInfo> GetSymbolInfo(string exchange, string ticker)
	{
		var symbolInfo = await ExchangeStore.GetSymbolInfoLazy(exchange, ticker) ?? throw new Exception($"Could not find symbol info for {ticker}");
		return symbolInfo;
	}

	protected async Task<AMOrderBook> GetOrderBook(string exchange, string ticker)
	{
		var symbolInfo = await ExchangeStore.GetOrderBook(exchange, ticker);
		return symbolInfo;
	}

	public abstract Task<AMProviderResult> ExecuteOrder(ADBot bot, AMOrderRequest order);

	public abstract Task<AMProviderResult> ChangeTickersMarginType(List<string> tickers, MarginType type);

	public virtual void Dispose()
	{
		// Db.Dispose();
	}
}
