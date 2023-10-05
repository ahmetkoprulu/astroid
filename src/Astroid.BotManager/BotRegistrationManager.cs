using System.Collections.Concurrent;
using Astroid.Core.MessageQueue;
using Astroid.Entity;
using Microsoft.EntityFrameworkCore;

namespace Astroid.Core.Helpers;

public class BotRegistrationManager : IDisposable
{
	private IServiceProvider ServiceProvider { get; set; }
	private IMessageQueue MessageQueue { get; set; }
	private AQBotRegistration BotRegistration { get; set; }
	private ConcurrentDictionary<Guid, AQOrder> Registrations { get; set; } = new();

	public BotRegistrationManager(IServiceProvider serviceProvider, IMessageQueue messageQueue)
	{
		ServiceProvider = serviceProvider;
		MessageQueue = messageQueue;
		BotRegistration = new AQBotRegistration(MessageQueue);
	}

	public async Task Setup(CancellationToken cancellationToken = default)
	{
		await BotRegistration.SetupExchanges(cancellationToken);
		await BotRegistration.SetupQueue(cancellationToken);
	}

	public Task SubscribeToRegistrations(Func<AQOrderMessage, CancellationToken, Task> callback, CancellationToken cancellationToken = default)
		=> BotRegistration.SubscribeToRegistrations((msg, ct) => Register(msg, callback, ct), cancellationToken);

	public Task SubscribeToUnregistrations(CancellationToken cancellationToken = default)
		=> BotRegistration.SubscribeToUnregistrations(UnRegister, cancellationToken);

	public async Task Register(AQBotRegistrationMessage msg, Func<AQOrderMessage, CancellationToken, Task> callback, CancellationToken cancellationToken)
	{
		var scope = ServiceProvider.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AstroidDb>();

		var bot = await db.Bots.FirstOrDefaultAsync(x => x.Id == msg.BotId, cancellationToken);
		if (bot == null) return;

		bot.IsEnabled = true;

		var botOrders = new AQOrder(MessageQueue);
		await botOrders.Setup(bot.Id, cancellationToken);
		await botOrders.Subscribe(bot.Id, callback, cancellationToken);

		Registrations.TryAdd(bot.Id, botOrders);
	}

	public async Task UnRegister(AQBotRegistrationMessage msg, CancellationToken cancellationToken)
	{
		var isExist = Registrations.TryGetValue(msg.BotId, out var reg);
		if (!isExist) return;

		var scope = ServiceProvider.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AstroidDb>();

		var bot = await db.Bots.FirstOrDefaultAsync(x => x.Id == msg.BotId, cancellationToken);
		if (bot == null) return;

		reg?.Dispose();
		Registrations.TryRemove(bot.Id, out var _);
	}

	public AQOrder? TryGetOrderQueue(Guid botId)
	{
		var isExist = Registrations.TryGetValue(botId, out var reg);
		if (!isExist) return null;

		return reg;
	}

	public IEnumerable<AQOrder> GetRegistrations() => Registrations.Values;

	public void Dispose()
	{
		foreach (var reg in Registrations)
		{
			reg.Value?.Dispose();
		}

		GC.SuppressFinalize(this);
	}
}
