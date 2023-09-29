
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
		var settings = config.Get<AConfAppSettings>() ?? new();
		var connStringEnvVariable = Environment.GetEnvironmentVariable("ASTROID_MQ_CONNECTION_STRING");
		ConnectionString = connStringEnvVariable ?? settings.MessageQueue.ConnectionString;
		Bus = RabbitHutch.CreateBus(ConnectionString ?? throw new NullReferenceException("Invalid Message Queue Connection String"));
	}

	public async Task CreateExchange(string exchangeName, string exchangeType, bool durable = true, CancellationToken cancellationToken = default)
	{
		exchangeName = string.Format(ExchangeFormat, exchangeName);
		var exchange = new Exchange(exchangeName, exchangeType, durable);
		await Bus.Advanced.ExchangeDeclareAsync(
			exchangeName,
			c =>
				{
					c.AsDurable(durable);
					c.WithType(exchangeType);
				},
			cancellationToken
		);
	}

	public async Task CreateQueue(string queueName, bool durable = true, CancellationToken cancellationToken = default)
	{
		queueName = string.Format(QueueFormat, queueName);
		var queue = new Queue(queueName, durable);
		await Bus.Advanced.QueueDeclareAsync(
			queueName,
			c =>
				{
					c.AsDurable(durable);
				},
			cancellationToken
		);
	}

	public async Task<AMMessageQueueResult> Publish<T>(string queueName, T message, CancellationToken cancellationToken = default)
	{
		var exchangeName = string.Format(ExchangeFormat, queueName);
		var exchange = new Exchange(exchangeName);
		var queue = string.Format(QueueFormat, queueName);
		var msg = new Message<T>(message);
		try
		{
			await Bus.Advanced.PublishAsync(
				exchange,
				queue,
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

	public async Task<IDisposable> Subscribe<T>(string queueName, Func<T, CancellationToken, Task> callback, CancellationToken cancellationToken = default)
	{
		var exchangeName = string.Format(ExchangeFormat, queueName);
		var exchange = new Exchange(exchangeName);
		queueName = string.Format(QueueFormat, queueName);
		var queue = new Queue(queueName);

		await Bus.Advanced.BindAsync(exchange, queue, queueName, cancellationToken);

		var consumerCancellation = Bus.Advanced.Consume<T>(
			queue,
			(message, _) => callback(message.Body!, cancellationToken)
		);

		return new SubscriptionResult(exchange, queue, consumerCancellation);
	}


	public Task<IDisposable> Subscribe<T>(string queueName, Func<T, CancellationToken, Task> callback, Func<Exception, Task> errorCallback, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
