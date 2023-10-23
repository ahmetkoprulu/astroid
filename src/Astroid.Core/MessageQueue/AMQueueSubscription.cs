namespace Astroid.Core.MessageQueue;

public class AMQueueSubscription : IDisposable
{
	public Guid Id { get; set; }
	public string QueueName { get; set; }
	public IDisposable Subscription { get; set; }

	public AMQueueSubscription(Guid id, string queueName, IDisposable subscription)
	{
		Id = id;
		QueueName = queueName;
		Subscription = subscription;
	}


	public AMQueueSubscription(string queueName, IDisposable subscription)
	{
		QueueName = queueName;
		Subscription = subscription;
	}

	public void Dispose()
	{
		Subscription.Dispose();
		GC.SuppressFinalize(this);
	}
}
