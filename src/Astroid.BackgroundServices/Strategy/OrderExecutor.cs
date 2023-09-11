
using Astroid.Core.Cache;
using Astroid.Core;
using Astroid.Entity;
using Astroid.Providers;
using Microsoft.EntityFrameworkCore;
using Astroid.Entity.Extentions;
using Astroid.Core.MessageQueue;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Astroid.BackgroundServices.Cache;

public class OrderExecutor : IHostedService
{
	private IServiceProvider ServiceProvider { get; set; }
	private ExchangeInfoStore ExchangeStore { get; set; }
	private ICacheService Cache { get; set; }
	private ILogger<OrderExecutor> Logger { get; set; }
	private IServiceProvider Services { get; set; }
	private AstroidDb Db { get; set; }
	private AQOrder OrderQueue { get; set; }

	public OrderExecutor(IServiceProvider serviceProvider, AstroidDb db, ExchangeInfoStore exchangeStore, AQOrder orderQueue, ICacheService cache, ILogger<OrderExecutor> logger, IServiceProvider services)
	{
		ServiceProvider = serviceProvider;
		Db = db;
		ExchangeStore = exchangeStore;
		OrderQueue = orderQueue;
		Cache = cache;
		Logger = logger;
		Services = services;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		Logger.LogInformation("Starting Order Executor Service.");
		_ = Task.Run(() => DoJob(cancellationToken), cancellationToken);

		return Task.CompletedTask;
	}

	public async Task DoJob(CancellationToken cancellationToken)
	{
		await OrderQueue.Subscribe(ExecuteOrder, cancellationToken);
		while (!cancellationToken.IsCancellationRequested)
		{
			await Task.Delay(1000 * 60 * 10, cancellationToken);
		}
	}

	public async Task ExecuteOrder(Guid orderId)
	{
		Logger.LogInformation($"Executing order {orderId}.");
		var order = await GetOrder(orderId);
		if (order == null)
		{
			Logger.LogError($"Order not found for {orderId}.");
			return;
		}

		var exchanger = ExchangerFactory.Create(ServiceProvider, order.Exchange);
		if (exchanger == null)
		{
			await AddAudit(order, AuditType.OpenOrderPlaced, $"Exchanger type {order.Exchange.Label} not found", order.Position.Id.ToString());
			return;
		}

		var request = GenerateRequest(order);
		var result = await exchanger.ExecuteOrder(order.Bot, request);
		result.Audits.ForEach(x =>
		{
			x.UserId = order.UserId;
			x.ActorId = order.BotId;
			x.TargetId = order.Id;
			x.CorrelationId = result.CorrelationId;
			Db.Audits.Add(x);
		});

		if (!result.Success)
		{
			Logger.LogError($"Order execution failed for {order.Id}.");
			order.Status = OrderStatus.Rejected;
			order.UpdatedDate = DateTime.UtcNow;
			await Db.SaveChangesAsync();

			return;
		}

		order.Status = OrderStatus.Filled;
		order.UpdatedDate = DateTime.UtcNow;
		await Db.SaveChangesAsync();
	}

	public AMOrderRequest GenerateRequest(ADOrder order)
	{
		var request = new AMOrderRequest
		{
			Ticker = order.Symbol,
			Leverage = order.Position.Leverage,
			Type = GetType(order),
			Quantity = order.Quantity,
			Key = order.Bot.Key
		};

		return request;
	}

	public string GetType(ADOrder order)
	{
		switch (order.TriggerType)
		{
			case OrderTriggerType.StopLoss:
				return order.Position.Type == PositionType.Long ? "close-long" : "close-short";
			case OrderTriggerType.TakeProfit:
				return order.Position.Type == PositionType.Long ? "close-long" : "close-short";
			case OrderTriggerType.Pyramiding:
				return order.Position.Type == PositionType.Long ? "open-long" : "open-short";
			default:
				throw new InvalidDataException("Invalid order trigger type.");
		}
	}

	public async Task<ADOrder?> GetOrder(Guid orderId, CancellationToken cancellationToken = default) =>
		await Db.Orders
			.AsNoTracking()
			.Include(x => x.Position)
			.Include(x => x.Bot)
			.Include(x => x.Exchange)
				.ThenInclude(x => x.Provider)
			.FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);

	public async Task AddAudit(ADOrder order, AuditType type, string description, string? corellationId = null) => await Db.AddAudit(order.UserId, order.BotId, type, description, order.Id, corellationId);

	public Task StopAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}
