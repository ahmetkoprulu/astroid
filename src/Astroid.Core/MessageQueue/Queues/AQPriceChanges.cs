using EasyNetQ.Topology;

namespace Astroid.Core.MessageQueue;

public class AQPriceChanges : IDisposable
{
	public IMessageQueue Mq { get; set; }
	public const string QueueLabel = "PriceChanges";
	public const string ExchangeLabel = "PriceChange";
	public Exchange Exchange { get; set; }
	public Queue Queue { get; set; }
	public IDisposable? Subscription { get; set; }

	public AQPriceChanges(IMessageQueue mq) => Mq = mq;

	public async Task Setup(CancellationToken cancellationToken = default)
	{
		Exchange = await Mq.CreateExchange(ExchangeLabel, "direct", true, cancellationToken);
		Queue = await Mq.CreateQueue(Exchange, QueueLabel, false, cancellationToken);
	}

	public async Task<AMMessageQueueResult> Publish(AQPriceChangeMessage message, CancellationToken cancellationToken = default) =>
		await Mq.Publish(Exchange, Queue, message, cancellationToken);

	public static async Task<AMMessageQueueResult> Publish(IMessageQueue mq, AQPriceChangeMessage message, string routingKey, CancellationToken cancellationToken = default)
	{
		try
		{
			var exchange = await mq.CreateExchange(ExchangeLabel, "direct", true, cancellationToken);
			var queue = await mq.CreateQueue(exchange, QueueLabel, false, cancellationToken);

			return await mq.Publish(exchange, queue, message, cancellationToken);
		}
		catch (Exception ex)
		{
			return new AMMessageQueueResult(false, ex.Message);
		}
	}

	public async Task<AMQueueSubscription> Subscribe(Func<AQPriceChangeMessage, CancellationToken, Task> action, CancellationToken cancellationToken = default)
	{
		var subscription = await Mq.Subscribe(Exchange, Queue, action, cancellationToken);
		Subscription = subscription;
		return new AMQueueSubscription(Queue.Name, subscription);
	}

	public void Dispose()
	{
		Subscription?.Dispose();
		GC.SuppressFinalize(this);
	}
}

