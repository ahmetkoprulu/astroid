using System;
using System.Threading;

namespace Astroid.Core.MessageQueue;

public interface IMessageQueue
{
	Task<AMMessageQueueResult> Publish<T>(string queueName, T message, CancellationToken cancellationToken = default);
	Task Subscribe<T>(string queueName, Func<T, Task> callback, CancellationToken cancellationToken = default);
	Task Subscribe<T>(string queueName, Func<T, Task> callback, Func<Exception, Task> errorCallback, CancellationToken cancellationToken = default);
}
