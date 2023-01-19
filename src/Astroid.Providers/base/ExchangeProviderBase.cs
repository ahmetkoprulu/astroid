using System.ComponentModel;
using System.Reflection;
using Astroid.Core;
using Astroid.Entity;
using Newtonsoft.Json;

namespace Astroid.Providers;

public abstract class ExchangeProviderBase : IDisposable
{
	protected ADExchange Exchange { get; set; }

	protected ExchangeProviderBase() { }

	public virtual void Context(string settings, ADExchange exchange)
	{
		Exchange = exchange;
	}

	public abstract Task ExecuteOrder(ADBot bot, OrderRequest order);

	public abstract void Dispose();
}
