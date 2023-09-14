namespace Astroid.Core.MessageQueue;

public class AQOrder
{
	public IMessageQueue Mq { get; set; }
	public const string Label = "Orders";

	public AQOrder(IMessageQueue mq) => Mq = mq;

	public async Task Setup(CancellationToken cancellationToken = default)
	{
		await Mq.CreateExchange(Label, "direct", true, cancellationToken);
		await Mq.CreateQueue(Label, true, cancellationToken);
	}

	public async Task<AMMessageQueueResult> Publish(AQOrderMessage message, CancellationToken cancellationToken = default) => await Mq.Publish(Label, message, cancellationToken);
	public async Task<IDisposable> Subscribe(string id, Func<AQOrderMessage, CancellationToken, Task> action, CancellationToken cancellationToken = default) => await Mq.Subscribe(Label, action, cancellationToken);
}

public class AQOrderMessage
{
	public Guid OrderId { get; set; }
}
