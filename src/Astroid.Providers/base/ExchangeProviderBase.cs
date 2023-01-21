using System.ComponentModel;
using System.Reflection;
using Astroid.Core;
using Astroid.Entity;
using Newtonsoft.Json;

namespace Astroid.Providers;

public abstract class ExchangeProviderBase : IDisposable
{
	public IServiceProvider ServiceProvider { get; set; }
	protected AstroidDb Db { get; set; }
	protected ADExchange Exchange { get; set; }

	protected ExchangeProviderBase() { }

	protected ExchangeProviderBase(IServiceProvider serviceProvider, ADExchange exchange)
	{
		ServiceProvider = serviceProvider;
		// Db = serviceProvider.GetService(typeof(AstroidDb)) as AstroidDb ?? throw new Exception("Db cannot be null");
		Exchange = exchange;
	}

	public virtual void Context()
	{

	}

	public abstract Task<AMProviderResult> ExecuteOrder(ADBot bot, AMOrderRequest order);

	public virtual void Dispose()
	{
		// Db.Dispose();
	}
}
