
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using EasyNetQ;
using Microsoft.Extensions.Configuration;

namespace Astroid.Core.MessageQueue;

public class RabbitMessageQueue : IMessageQueue
{
	private string ConnectionString { get; set; }  // amqp://user:pass@hostName:port/vhost
	private IBus Bus { get; set; }

	public RabbitMessageQueue(IConfiguration config)
	{
		var settings = config.Get<AConfAppSettings>() ?? new();
		var connStringEnvVariable = Environment.GetEnvironmentVariable("ASTROID_MQ_CONNECTION_STRING");
		ConnectionString = connStringEnvVariable ?? settings.Cache.ConnectionString;
		Bus = RabbitHutch.CreateBus(ConnectionString ?? throw new NullReferenceException("Invalid Message Queue Connection String"));
	}

	public async Task<AMMessageQueueResult> Publish<T>(string queueName, T message, CancellationToken cancellationToken = default)
	{
		try
		{
			await Bus.PubSub.PublishAsync<T>(message, queueName, cancellationToken);
			return new(true);
		}
		catch (Exception ex)
		{
			return new(false, ex.Message);
		}
	}
	public async Task Subscribe<T>(string queueName, Func<T, Task> callback, CancellationToken cancellationToken = default) => await Bus.PubSub.SubscribeAsync<T>(queueName, callback, cancellationToken);

	public Task Subscribe<T>(string queueName, Func<T, Task> callback, Func<Exception, Task> errorCallback, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
