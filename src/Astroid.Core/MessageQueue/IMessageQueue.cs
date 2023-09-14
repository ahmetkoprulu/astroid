using System;
using System.Threading;

namespace Astroid.Core.MessageQueue;

public interface IMessageQueue
{
	Task<AMMessageQueueResult> Publish<T>(string queueName, T message, CancellationToken cancellationToken = default);
	Task<IDisposable> Subscribe<T>(string queueName, Func<T, CancellationToken, Task> callback, CancellationToken cancellationToken = default);
	Task<IDisposable> Subscribe<T>(string queueName, Func<T, CancellationToken, Task> callback, Func<Exception, Task> errorCallback, CancellationToken cancellationToken = default);
	Task CreateExchange(string exchangeName, string exchangeType, bool durable = true, CancellationToken cancellationToken = default);
	Task CreateQueue(string queueName, bool durable = true, CancellationToken cancellationToken = default);
}
