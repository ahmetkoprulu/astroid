using Microsoft.AspNetCore.Mvc;
using Astroid.Entity;
using Astroid.Web.Models;
using Astroid.Core;
using Microsoft.EntityFrameworkCore;
using Astroid.Providers;
using Astroid.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Astroid.Core.Cache;
using Binance.Net.Enums;

namespace Astroid.Web;

public class BotsController : SecureController
{
	private readonly IServiceProvider ServiceProvider;

	public BotsController(IServiceProvider serviceProvider, AstroidDb db, ICacheService cache, ILogger<BotsController> logger) : base(db, cache)
	{
		ServiceProvider = serviceProvider;
		Logger = logger;
	}

	[HttpPost("list")]
	public async Task<IActionResult> List([FromBody] MPViewDataList<ADBot> model)
	{
		model ??= new MPViewDataList<ADBot>();

		model = await Db.Bots
			.Where(x => x.UserId == CurrentUser.Id)
			.AsNoTracking()
			.OrderByDescending(x => x.CreatedDate)
			.ViewDataListAsync<ADBot>(model);

		return Success(model.ForJson(x => new AMBot
		{
			Id = x.Id,
			Label = x.Label,
			Description = x.Description,
		}));
	}

	[HttpGet("{id}")]
	public async Task<IActionResult> Get(Guid id)
	{
		if (id == Guid.Empty)
			return BadRequest("Invalid exchange id");

		var bot = await Db.Bots
			.Where(x => x.UserId == CurrentUser.Id)
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == id);
		if (bot == null)
			return NotFound("Exchange not found");

		return Success(new AMBot
		{
			Id = bot.Id,
			Label = bot.Label,
			Description = bot.Description,
			ExchangeId = bot.ExchangeId,
			OrderType = bot.OrderType,
			OrderMode = bot.OrderMode,
			PositionSizeType = bot.PositionSizeType,
			PositionSize = bot.PositionSize,
			IsPositionSizeExpandable = bot.IsPositionSizeExpandable,
			IsTakePofitEnabled = bot.IsTakePofitEnabled,
			TakeProfitTargets = bot.TakeProfitTargets,
			IsStopLossEnabled = bot.IsStopLossEnabled,
			StopLossPrice = bot.StopLossPrice,
			StopLossCallbackRate = bot.StopLossCallbackRate,
			StopLossActivation = bot.StopLossActivation,
			Key = bot.Key,
			IsEnabled = bot.IsEnabled,
			LimitSettings = bot.LimitSettings
		});
	}

	[HttpPost("save")]
	public async Task<IActionResult> Save([FromBody] AMBot model)
	{
		var isNameExists = await Db.Bots.AnyAsync(x => x.Label == model.Label && x.Id != model.Id);
		if (isNameExists) BadRequest("Exchange name already exists");

		if (model.Id == Guid.Empty)
		{
			var bot = new ADBot
			{
				Id = Guid.NewGuid(),
				Label = model.Label,
				Description = model.Description,
				ExchangeId = model.ExchangeId,
				OrderType = model.OrderType,
				OrderMode = model.OrderMode,
				PositionSizeType = model.PositionSizeType,
				PositionSize = model.PositionSize,
				IsPositionSizeExpandable = model.IsPositionSizeExpandable,
				IsTakePofitEnabled = model.IsTakePofitEnabled,
				TakeProfitTargets = model.TakeProfitTargets,
				IsStopLossEnabled = model.IsStopLossEnabled,
				StopLossPrice = model.StopLossPrice,
				StopLossCallbackRate = model.StopLossCallbackRate,
				StopLossActivation = model.StopLossActivation,
				Key = model.Key,
				CreatedDate = DateTime.Now,
				UserId = CurrentUser.Id,
				IsEnabled = model.IsEnabled,
				LimitSettings = model.LimitSettings
			};

			await Db.Bots.AddAsync(bot);
		}
		else
		{
			var bot = await Db.Bots.FirstOrDefaultAsync(x => x.Id == model.Id);
			if (bot == null)
				return NotFound("Exchange not found");

			bot.Label = model.Label;
			bot.Description = model.Description;
			bot.ExchangeId = model.ExchangeId;
			bot.OrderType = model.OrderType;
			bot.OrderMode = model.OrderMode;
			bot.PositionSizeType = model.PositionSizeType;
			bot.PositionSize = model.PositionSize;
			bot.IsPositionSizeExpandable = model.IsPositionSizeExpandable;
			bot.IsTakePofitEnabled = model.IsTakePofitEnabled;
			bot.TakeProfitTargets = model.TakeProfitTargets;
			bot.IsStopLossEnabled = model.IsStopLossEnabled;
			bot.StopLossPrice = model.StopLossPrice;
			bot.StopLossCallbackRate = model.StopLossCallbackRate;
			bot.StopLossActivation = model.StopLossActivation;
			bot.Key = model.Key;
			bot.IsEnabled = model.IsEnabled;
			bot.ModifiedDate = DateTime.Now;
			bot.LimitSettings = model.LimitSettings;
		}

		await Db.SaveChangesAsync();

		return Success("Exchange saved successfully");
	}

	[AllowAnonymous]
	[HttpPost("execute")]
	public async Task<IActionResult> Execute([FromBody] AMOrderRequest orderRequest)
	{
		var key = orderRequest?.Key ?? string.Empty;
		var bot = await Db.Bots.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key);
		if (bot == null) return BadRequest($"Bot {key} not found");

		if (!bot.IsEnabled)
		{
			await AddAudit(AuditType.OrderRequest, bot.UserId, bot.Id, $"Bot is disabled");
			return BadRequest("Bot is disabled");
		}

		if (orderRequest == null || string.IsNullOrEmpty(orderRequest.Ticker) || string.IsNullOrEmpty(orderRequest.Type))
		{
			await AddAudit(AuditType.OrderRequest, bot.UserId, bot.Id, $"Invalid request model");
			return BadRequest($"Invalid request model");
		}

		var exchange = await Db.Exchanges
			.AsNoTracking()
			.Include(x => x.Provider)
			.FirstOrDefaultAsync(x => x.Id == bot.ExchangeId);
		if (exchange == null)
		{
			await AddAudit(AuditType.OrderRequest, bot.UserId, bot.Id, $"Exchange {bot.ExchangeId} not found");
			return BadRequest($"Exchange {bot.ExchangeId} not found");
		}

		var exchanger = ExchangerFactory.Create(ServiceProvider, exchange);
		if (exchanger == null)
		{
			await AddAudit(AuditType.OrderRequest, bot.UserId, bot.Id, $"Exchanger type {exchange.Provider.Title} not found");
			return BadRequest($"Exchanger type {exchange.Provider.Title} not found");
		}

		try
		{
			if (Cache.IsLocked($"lock:bot:{bot.Id}"))
			{
				await AddAudit(AuditType.OrderRequest, bot.UserId, bot.Id, $"Order request rejected since the bot is already processing an order.");
				return BadRequest("Bot is busy");
			}

			var _ = Cache.AcquireLock($"lock:bot:{bot.Id}");
			var result = await exchanger.ExecuteOrder(bot, orderRequest);
			if (!result.Success) LogError(null, result.Message ?? string.Empty);

			result.Audits.ForEach(x =>
			{
				x.UserId = exchange.UserId;
				x.ActorId = bot.Id;
				Db.Audits.Add(x);
			});

			await Db.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			LogError(ex, ex.Message);
			return Error(ex.Message);
		}
		finally
		{
			Cache.ReleaseLock($"lock:bot:{bot.Id}");
		}

		return Success(null, "Order executed successfully");
	}

	[HttpPost("{ticker}/test-computation-method")]
	public async Task<IActionResult> TestComputationMethod(string ticker, [FromBody] AMBot bot)
	{
		if (string.IsNullOrEmpty(ticker))
			return BadRequest("Invalid ticker");

		var exchange = await Db.Exchanges
			.AsNoTracking()
			.Include(x => x.Provider)
			.FirstOrDefaultAsync(x => x.Id == bot.ExchangeId);

		if (exchange == null)
			return NotFound($"Exchange {bot.ExchangeId} not found");

		var symbolInfo = ExchangeInfoStore.GetSymbolInfo(exchange.Provider.Name, ticker);
		if (symbolInfo == null)
			return BadRequest($"Symbol {ticker} not found");

		var orderBook = symbolInfo.OrderBook;
		if (orderBook == null)
			return BadRequest($"Order book for {ticker} not found");

		var longResult = ExchangeProviderBase.GetEntryPoint(orderBook, PositionSide.Long, bot.LimitSettings);
		var shortResult = ExchangeProviderBase.GetEntryPoint(orderBook, PositionSide.Short, bot.LimitSettings);

		return Success(new
		{
			Long = Math.Round(longResult, symbolInfo.PricePrecision),
			Short = Math.Round(shortResult, symbolInfo.PricePrecision)
		});
	}

	[HttpPut("{id}/margin-type")]
	public async Task<IActionResult> SetMarginType(Guid id, [FromQuery(Name = "type")] MarginType type, [FromQuery(Name = "tickers")] string tickers)
	{
		if (id == Guid.Empty)
			return BadRequest("Invalid exchange id");

		if (string.IsNullOrEmpty(tickers))
			return BadRequest("Invalid tickers");

		var bot = await Db.Bots.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUser.Id);
		if (bot == null) return BadRequest($"Bot {id} not found");

		var exchange = await Db.Exchanges
			.AsNoTracking()
			.Include(x => x.Provider)
			.FirstOrDefaultAsync(x => x.Id == bot.ExchangeId);
		if (exchange == null)
		{
			await AddAudit(AuditType.ChangeMarginType, bot.UserId, bot.Id, $"Exchange {bot.ExchangeId} not found");
			return BadRequest($"Exchange {bot.ExchangeId} not found");
		}

		var exchanger = ExchangerFactory.Create(ServiceProvider, exchange);
		if (exchanger == null)
		{
			await AddAudit(AuditType.ChangeMarginType, exchange.UserId, bot.Id, $"Exchanger type {exchange.Provider.Title} not found");
			return BadRequest($"Exchanger type {exchange.Provider.Title} not found");
		}

		try
		{
			var tickerList = tickers.Split(',').ToList();
			var result = await exchanger.ChangeTickersMarginType(tickerList, type);
			if (!result.Success) LogError(null, result.Message ?? string.Empty);

			result.Audits.ForEach(x =>
			{
				x.UserId = exchange.UserId;
				x.ActorId = bot.Id;
				Db.Audits.Add(x);
			});

			await Db.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			LogError(ex, ex.Message);
			return Error(ex.Message);
		}

		await Db.SaveChangesAsync();

		return Success("Margin type saved successfully");
	}

	[HttpDelete("{id}")]
	public async Task<IActionResult> Delete(Guid id)
	{
		if (id == Guid.Empty)
			return BadRequest("Invalid exchange id");

		var bot = await Db.Bots.Where(x => x.UserId == CurrentUser.Id).FirstOrDefaultAsync(x => x.Id == id);
		if (bot == null)
			return NotFound("Exchange not found");

		Db.Bots.Remove(bot);
		await Db.SaveChangesAsync();

		return Success("Exchange deleted successfully");
	}

	[NonAction]
	public async Task AddAudit(AuditType type, Guid userId, Guid botId, string description)
	{
		var audit = new ADAudit
		{
			Id = Guid.NewGuid(),
			UserId = userId,
			ActorId = botId,
			Type = type,
			Description = description,
			CorrelationId = ExchangeProviderBase.GenerateCorrelationId(),
			CreatedDate = DateTime.UtcNow,
		};

		await Db.Audits.AddAsync(audit);
		await Db.SaveChangesAsync();
	}
}
