
using CryptoExchange.Net;
using EasyNetQ;
using EasyNetQ.Internals;
using EasyNetQ.Topology;
using Microsoft.Extensions.Configuration;

namespace Astroid.Core.MessageQueue;

public class RabbitMessageQueue : IMessageQueue
{
	private string ConnectionString { get; set; }  // amqp://user:pass@hostName:port/vhost
	private IBus Bus { get; set; }
	private const string ExchangeFormat = "Astroid.{0}.Exchange";
	private const string QueueFormat = "Astroid.{0}.Queue";

	public RabbitMessageQueue(IConfiguration config)
	{
		var settings = config.Get<WebConfig>() ?? new();
		var connStringEnvVariable = Environment.GetEnvironmentVariable("ASTROID_MQ_CONNECTION_STRING");
		ConnectionString = connStringEnvVariable ?? settings.MessageQueue.ConnectionString;
		Bus = RabbitHutch.CreateBus(ConnectionString ?? throw new NullReferenceException("Invalid Message Queue Connection String"));
	}

	public async Task<Exchange> CreateExchange(string exchangeName, string exchangeType, bool durable = true, CancellationToken cancellationToken = default)
	{
		exchangeName = string.Format(ExchangeFormat, exchangeName);
		return await Bus.Advanced.ExchangeDeclareAsync(
			exchangeName,
			c =>
				{
					c.AsDurable(durable);
					c.WithType(exchangeType);
				},
			cancellationToken
		);
	}

	public async Task<Queue> CreateQueue(Exchange exchange, string queueName, bool durable = true, CancellationToken cancellationToken = default)
	{
		queueName = string.Format(QueueFormat, queueName);
		var queue = await Bus.Advanced.QueueDeclareAsync(
			queueName,
			c =>
				{
					c.AsDurable(durable);
				},
			cancellationToken
		);
		await Bus.Advanced.BindAsync(exchange, queue, queueName, cancellationToken);

		return queue;
	}

	public async Task<AMMessageQueueResult> Publish<T>(Exchange exchange, Queue queue, T message, CancellationToken cancellationToken = default)
	{
		var msg = new Message<T>(message);

		try
		{
			await Bus.Advanced.PublishAsync(
				exchange,
				queue.Name,
				false,
				msg,
				cancellationToken
			);

			return new(true);
		}
		catch (Exception ex)
		{
			return new(false, ex.Message);
		}
	}

	public Task<IDisposable> Subscribe<T>(Exchange exchange, Queue queue, Func<T, CancellationToken, Task> callback, CancellationToken cancellationToken = default)
	{
		var consumerCancellation = Bus.Advanced.Consume<T>(
			queue,
			(message, _) => callback(message.Body!, cancellationToken)
		);

		return Task.FromResult(new SubscriptionResult(exchange, queue, consumerCancellation) as IDisposable);
	}
}
