namespace Astroid.Core.MessageQueue;

public class AQOrder
{
	public IMessageQueue Mq { get; set; }
	public const string QueueName = "order-execution-queue";

	public AQOrder(IMessageQueue mq) => Mq = mq;

	public async Task<AMMessageQueueResult> Publish(AQOrderMessage message, CancellationToken cancellationToken = default) => await Mq.Publish(QueueName, message, cancellationToken);
}

public class AQOrderMessage
{
	public Guid OrderId { get; set; }
}
