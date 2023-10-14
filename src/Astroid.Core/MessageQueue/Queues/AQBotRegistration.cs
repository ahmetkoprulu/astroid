using EasyNetQ.Topology;

namespace Astroid.Core.MessageQueue;

public class AQBotRegistration : IDisposable
{
	public IMessageQueue Mq { get; set; }

	public const string RegistrationLabel = "BotRegistrations";
	public Exchange RegistrationExchange { get; set; }
	public Queue RegistrationQueue { get; set; }
	public IDisposable? RegistrationSubscription { get; set; }

	public const string UnregisterationLabel = "BotUnregistrations";
	public Exchange UnregistrationExchange { get; set; }
	public Queue UnregistrationQueue { get; set; }
	public IDisposable? UnregistrationSubscription { get; set; }

	public AQBotRegistration(IMessageQueue mq) => Mq = mq;

	public async Task SetupExchanges(CancellationToken cancellationToken = default)
	{
		RegistrationExchange = await Mq.CreateExchange(RegistrationLabel, "direct", true, cancellationToken);
		UnregistrationExchange = await Mq.CreateExchange(UnregisterationLabel, "fanout", true, cancellationToken);
	}

	public async Task SetupRegistrationExchange(CancellationToken cancellationToken = default)
		=> RegistrationExchange = await Mq.CreateExchange(RegistrationLabel, "direct", true, cancellationToken);

	public async Task SetupUnregistrationExchange(CancellationToken cancellationToken = default)
		=> UnregistrationExchange = await Mq.CreateExchange(UnregisterationLabel, "fanout", true, cancellationToken);

	public async Task SetupQueue(CancellationToken cancellationToken = default)
	{
		RegistrationQueue = await Mq.CreateQueue(RegistrationExchange, RegistrationLabel, false, cancellationToken);
		UnregistrationQueue = await Mq.CreateQueue(UnregistrationExchange, UnregisterationLabel, false, cancellationToken);
	}

	public async Task SetupRegistrationQueue(CancellationToken cancellationToken = default)
		=> RegistrationQueue = await Mq.CreateQueue(RegistrationExchange, RegistrationLabel, false, cancellationToken);

	public async Task SetupUnregistrationQueue(CancellationToken cancellationToken = default)
		=> UnregistrationQueue = await Mq.CreateQueue(UnregistrationExchange, UnregisterationLabel, false, cancellationToken);

	public async Task<AMMessageQueueResult> PublishRegistration(AQBotRegistrationMessage message, CancellationToken cancellationToken = default)
		=> await Mq.Publish(RegistrationExchange, RegistrationQueue, message, cancellationToken);

	public async Task<AMMessageQueueResult> PublishUnregistration(AQBotRegistrationMessage message, CancellationToken cancellationToken = default)
		=> await Mq.Publish(UnregistrationExchange, UnregistrationQueue, message, cancellationToken);

	public static async Task<AMMessageQueueResult> PublishRegistration(IMessageQueue mq, AQBotRegistrationMessage message, CancellationToken cancellationToken = default)
	{
		var exchange = new Exchange(RegistrationLabel);
		var queue = new Queue(RegistrationLabel);

		return await mq.Publish(exchange, queue, message, cancellationToken);
	}

	public static async Task<AMMessageQueueResult> PublishUnregistration(IMessageQueue mq, AQBotRegistrationMessage message, CancellationToken cancellationToken = default)
	{
		var exchange = new Exchange(UnregisterationLabel);
		var queue = new Queue(UnregisterationLabel);

		return await mq.Publish(exchange, queue, message, cancellationToken);
	}

	public async Task<AMQueueSubscription> SubscribeToRegistrations(Func<AQBotRegistrationMessage, CancellationToken, Task> action, CancellationToken cancellationToken = default)
	{
		RegistrationSubscription = await Mq.Subscribe(RegistrationExchange, UnregistrationQueue, action, cancellationToken);
		return new AMQueueSubscription(Guid.NewGuid(), RegistrationLabel, RegistrationSubscription);
	}

	public async Task<AMQueueSubscription> SubscribeToUnregistrations(Func<AQBotRegistrationMessage, CancellationToken, Task> action, CancellationToken cancellationToken = default)
	{
		UnregistrationSubscription = await Mq.Subscribe(UnregistrationExchange, UnregistrationQueue, action, cancellationToken);
		return new AMQueueSubscription(Guid.NewGuid(), UnregistrationQueue.Name, UnregistrationSubscription);
	}

	public void Dispose()
	{
		RegistrationSubscription?.Dispose();
		UnregistrationSubscription?.Dispose();

		GC.SuppressFinalize(this);
	}
}

public class AQBotRegistrationMessage
{
	public Guid BotId { get; set; }
	public BotRegistrationType Type { get; set; }
}

public enum BotRegistrationType : short
{
	Unknown = 0,
	Register = 1,
	Unregister = 2
}
