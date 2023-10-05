using System;
using System.Threading;
using EasyNetQ.Topology;

namespace Astroid.Core.MessageQueue;

public interface IMessageQueue
{
	Task<AMMessageQueueResult> Publish<T>(Exchange exchange, Queue queue, T message, CancellationToken cancellationToken = default);
	Task<IDisposable> Subscribe<T>(Exchange exchange, Queue queue, Func<T, CancellationToken, Task> callback, CancellationToken cancellationToken = default);
	Task<Exchange> CreateExchange(string exchangeName, string exchangeType, bool durable = true, CancellationToken cancellationToken = default);
	Task<Queue> CreateQueue(Exchange exchange, string queueName, bool durable = true, CancellationToken cancellationToken = default);
}
