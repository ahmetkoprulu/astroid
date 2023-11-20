using EasyNetQ.Topology;

namespace Astroid.Core.MessageQueue;

public class AQOrder : IDisposable
{
	public Guid Id { get; set; }
	public IMessageQueue Mq { get; set; }
	// public const string QueueLabel = "{0}.Orders";
	public const string QueueLabel = "Orders";
	public const string ExchangeLabel = "Orders";
	public Exchange Exchange { get; set; }
	public Queue Queue { get; set; }
	public IDisposable? Subscription { get; set; }

	public AQOrder(IMessageQueue mq) => Mq = mq;

	public async Task Setup(CancellationToken cancellationToken = default)
	{
		var queueName = string.Format(QueueLabel);
		Exchange = await Mq.CreateExchange(ExchangeLabel, "direct", true, cancellationToken);
		Queue = await Mq.CreateQueue(Exchange, queueName, false, cancellationToken);
	}

	public async Task<AMMessageQueueResult> Publish(AQOrderMessage message, CancellationToken cancellationToken = default) =>
		await Mq.Publish(Exchange, Queue, message, cancellationToken);

	public static async Task<AMMessageQueueResult> Publish(IMessageQueue mq, AQOrderMessage message, string routingKey, CancellationToken cancellationToken = default)
	{
		try
		{
			var queueName = string.Format(QueueLabel, routingKey);
			var exchange = await mq.CreateExchange(ExchangeLabel, "direct", true, cancellationToken);
			var queue = await mq.CreateQueue(exchange, queueName, false, cancellationToken);

			return await mq.Publish(exchange, queue, message, cancellationToken);
		}
		catch (Exception ex)
		{
			return new AMMessageQueueResult(false, ex.Message);
		}
	}

	public async Task<AMQueueSubscription> Subscribe(Func<AQOrderMessage, CancellationToken, Task> action, CancellationToken cancellationToken = default)
	{
		var subscription = await Mq.Subscribe(Exchange, Queue, action, cancellationToken);
		Subscription = subscription;
		return new AMQueueSubscription(Guid.NewGuid(), Queue.Name, subscription);
	}

	public void Dispose()
	{
		Subscription?.Dispose();
		GC.SuppressFinalize(this);
	}
}
