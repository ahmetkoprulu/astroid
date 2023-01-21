using Microsoft.AspNetCore.Mvc;
using Astroid.Entity;
using Astroid.Web.Models;
using Astroid.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Astroid.Providers;

namespace Astroid.Web;

public class StrategiesController : BaseController
{
	private readonly IServiceProvider ServiceProvider;

	public StrategiesController(IServiceProvider serviceProvider, AstroidDb db, ILogger<StrategiesController> logger) : base(db)
	{
		ServiceProvider = serviceProvider;
		Logger = logger;
	}

	[HttpPost("{key}/open-long")]
	public async Task<IActionResult> OpenLong(string key, [FromBody] AMOrderRequest orderRequest)
	{
		var bot = await Db.Bots.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key);
		if (bot == null) throw new Exception($"Bot {key} not found");

		var exchange = await Db.Exchanges
			.AsNoTracking()
			.Include(x => x.Provider)
			.FirstOrDefaultAsync(x => x.Id == bot.ExchangeId);
		if (exchange == null) throw new Exception($"Exchange {bot.ExchangeId} not found");

		var exchanger = ExchangerFactory.Create(ServiceProvider, exchange);
		if (exchanger == null) throw new Exception($"Exchanger type {exchange.Provider.Name} not found");

		if (orderRequest == null || string.IsNullOrEmpty(orderRequest.Ticker)) throw new Exception("Ticker is required");

		orderRequest.OrderType = OrderType.Buy;
		orderRequest.PositionType = PositionType.Long;

		var result = await exchanger.ExecuteOrder(bot, orderRequest);
		if (!result.Success) LogInfo(result.Message);

		result.Audits.ForEach(x =>
		{
			x.UserId = exchange.UserId;
			x.ActorId = bot.Id;
			Db.Audits.Add(x);
		});

		await Db.SaveChangesAsync();

		return Success(null, "Order executed successfully");
	}

	[HttpPost("{key}/close-long")]
	public async Task<IActionResult> CloseLong(string key, [FromBody] AMOrderRequest orderRequest)
	{
		var bot = await Db.Bots.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key);
		if (bot == null) throw new Exception($"Bot {key} not found");

		var exchange = await Db.Exchanges
			.AsNoTracking()
			.Include(x => x.Provider)
			.FirstOrDefaultAsync(x => x.Id == bot.ExchangeId);
		if (exchange == null) throw new Exception($"Exchange {bot.ExchangeId} not found");

		var exchanger = ExchangerFactory.Create(ServiceProvider, exchange);
		if (exchanger == null) throw new Exception($"Exchanger type {exchange.Provider.Name} not found");

		if (orderRequest == null || string.IsNullOrEmpty(orderRequest.Ticker)) throw new Exception("Ticker is required");

		orderRequest.OrderType = OrderType.Sell;
		orderRequest.PositionType = PositionType.Long;

		var result = await exchanger.ExecuteOrder(bot, orderRequest);
		if (!result.Success) LogInfo(result.Message);

		result.Audits.ForEach(x =>
		{
			x.UserId = exchange.UserId;
			x.ActorId = bot.Id;
			Db.Audits.Add(x);
		});

		await Db.SaveChangesAsync();

		return Success(null, "Order executed successfully");
	}

	[HttpPost("{key}/open-short")]
	public async Task<IActionResult> OpenShort(string key, [FromBody] AMOrderRequest orderRequest)
	{
		var bot = await Db.Bots.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key);
		if (bot == null) throw new Exception($"Bot {key} not found");

		var exchange = await Db.Exchanges
			.AsNoTracking()
			.Include(x => x.Provider)
			.FirstOrDefaultAsync(x => x.Id == bot.ExchangeId);
		if (exchange == null) throw new Exception($"Exchange {bot.ExchangeId} not found");

		var exchanger = ExchangerFactory.Create(ServiceProvider, exchange);
		if (exchanger == null) throw new Exception($"Exchanger type {exchange.Provider.Name} not found");

		if (orderRequest == null || string.IsNullOrEmpty(orderRequest.Ticker)) throw new Exception("Ticker is required");

		orderRequest.OrderType = OrderType.Sell;
		orderRequest.PositionType = PositionType.Short;

		var result = await exchanger.ExecuteOrder(bot, orderRequest);
		if (!result.Success) LogInfo(result.Message);

		result.Audits.ForEach(x =>
		{
			x.UserId = exchange.UserId;
			x.ActorId = bot.Id;
			Db.Audits.Add(x);
		});

		await Db.SaveChangesAsync();

		return Success(null, "Order executed successfully");
	}

	[HttpPost("{key}/close-short")]
	public async Task<IActionResult> CloseShort(string key, [FromBody] AMOrderRequest orderRequest)
	{
		var bot = await Db.Bots.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key);
		if (bot == null) throw new Exception($"Bot {key} not found");

		var exchange = await Db.Exchanges
			.AsNoTracking()
			.Include(x => x.Provider)
			.FirstOrDefaultAsync(x => x.Id == bot.ExchangeId);
		if (exchange == null) throw new Exception($"Exchange {bot.ExchangeId} not found");

		var exchanger = ExchangerFactory.Create(ServiceProvider, exchange);
		if (exchanger == null) throw new Exception($"Exchanger type {exchange.Provider.Name} not found");

		if (orderRequest == null || string.IsNullOrEmpty(orderRequest.Ticker)) throw new Exception("Ticker is required");

		orderRequest.OrderType = OrderType.Buy;
		orderRequest.PositionType = PositionType.Short;

		var result = await exchanger.ExecuteOrder(bot, orderRequest);
		if (!result.Success) LogInfo(result.Message);

		result.Audits.ForEach(x =>
		{
			x.UserId = exchange.UserId;
			x.ActorId = bot.Id;
			Db.Audits.Add(x);
		});

		await Db.SaveChangesAsync();

		return Success(null, "Order executed successfully");
	}
}