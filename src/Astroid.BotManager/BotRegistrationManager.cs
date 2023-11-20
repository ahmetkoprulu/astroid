using System.Collections.Concurrent;
using Astroid.Core.MessageQueue;
using Astroid.Entity;
using Microsoft.EntityFrameworkCore;

namespace Astroid.BotManager;

public class BotRegistrationManager : IDisposable
{
	private IServiceProvider ServiceProvider { get; set; }
	private AQOrder OrderQueue { get; set; }
	private ConcurrentDictionary<Guid, IDisposable> Consumers { get; set; } = new();

	public BotRegistrationManager(IServiceProvider serviceProvider, AQOrder orderQueue)
	{
		ServiceProvider = serviceProvider;
		OrderQueue = orderQueue;
	}

	public async Task Setup(CancellationToken cancellationToken = default) => await OrderQueue.Setup(cancellationToken);

	public async Task Subscribe(Func<AQOrderMessage, CancellationToken, Task> action, CancellationToken cancellationToken = default)
	{
		var consumer = await OrderQueue.Subscribe(action, cancellationToken);
		Consumers.TryAdd(consumer.Id, consumer);
	}

	public IEnumerable<IDisposable> GetRegistrations() => Consumers.Values;

	public void Dispose()
	{
		foreach (var reg in Consumers)
		{
			reg.Value?.Dispose();
		}

		GC.SuppressFinalize(this);
	}
}
