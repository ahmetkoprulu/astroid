using System.ComponentModel;
using System.Reflection;
using Astroid.Core;
using Astroid.Entity;
using Newtonsoft.Json;

namespace Astroid.Providers;

public abstract class ExchangeProviderBase : IDisposable
{
	public Guid SourceId { get; set; }
	public Guid ProviderId { get; set; }

	public ExchangeProviderBase() { }

	public virtual void Context(string settings)
	{

	}

	public abstract Task ExecuteOrder(ADBot bot, OrderRequest order);

	public abstract void Dispose();
}
