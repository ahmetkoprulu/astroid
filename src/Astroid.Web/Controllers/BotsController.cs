using Microsoft.AspNetCore.Mvc;
using Astroid.Entity;
using Astroid.Web.Models;
using Astroid.Core;
using Microsoft.EntityFrameworkCore;
using Astroid.Providers;
using Astroid.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Astroid.Core.Cache;
using Astroid.Core.MessageQueue;
using Astroid.Entity.Extentions;

namespace Astroid.Web;

public class BotsController : SecureController
{
	private ExchangeInfoStore ExchangeStore { get; set; }
	private readonly IServiceProvider ServiceProvider;

	public BotsController(IServiceProvider serviceProvider, AstroidDb db, ICacheService cache, ExchangeInfoStore exchangeStore, ILogger<BotsController> logger) : base(db, cache)
	{
		ServiceProvider = serviceProvider;
		Logger = logger;
		ExchangeStore = exchangeStore;
	}

	[HttpPost("list")]
	public async Task<IActionResult> List([FromBody] MPViewDataList<ADBot> model)
	{
		model ??= new MPViewDataList<ADBot>();

		model = await Db.Bots
			.Include(x => x.Exchange)
				.ThenInclude(x => x.Provider)
			.Where(x => x.UserId == CurrentUser.Id && !x.IsRemoved)
			.AsNoTracking()
			.OrderByDescending(x => x.CreatedDate)
			.ViewDataListAsync<ADBot>(model);

		return Success(model.ForJson(x => new AMBot
		{
			Id = x.Id,
			Label = x.Label,
			Description = x.Description,
			IsEnabled = x.IsEnabled,
			CreatedDate = x.CreatedDate,
			Exchange = new AMExchange
			{
				Id = x.Exchange.Id,
				Name = x.Exchange.Label,
				ProviderId = x.Exchange.Provider.Id,
				ProviderName = x.Exchange.Provider.Name,
				ProviderLabel = x.Exchange.Provider.Title,
			}
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
			IsPyramidingEnabled = bot.IsPyramidingEnabled,
			PyramidingSettings = bot.PyramidingSettings,
			IsTakePofitEnabled = bot.IsTakePofitEnabled,
			TakeProfitSettings = bot.TakeProfitSettings,
			IsStopLossEnabled = bot.IsStopLossEnabled,
			StopLossSettings = bot.StopLossSettings,
			Key = bot.Key,
			IsEnabled = bot.IsEnabled,
			LimitSettings = bot.LimitSettings
		});
	}

	[HttpGet]
	public async Task<IActionResult> Get()
	{
		var bots = await Db.Bots
			.Include(x => x.Exchange)
				.ThenInclude(x => x.Provider)
			.Where(x => x.UserId == CurrentUser.Id && !x.IsRemoved)
			.AsNoTracking()
			.OrderByDescending(x => x.CreatedDate)
			.Select(x => new AMBot
			{
				Id = x.Id,
				Label = x.Label,
				Description = x.Description,
				IsEnabled = x.IsEnabled,
				Key = x.Key,
				CreatedDate = x.CreatedDate,
				Exchange = new AMExchange
				{
					Id = x.Exchange.Id,
					Name = x.Exchange.Label,
					ProviderId = x.Exchange.Provider.Id,
					ProviderName = x.Exchange.Provider.Name,
					ProviderLabel = x.Exchange.Provider.Title,
				}
			})
			.ToListAsync();

		return Success(bots);
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
				IsPyramidingEnabled = model.IsPyramidingEnabled,
				PyramidingSettings = model.PyramidingSettings,
				IsTakePofitEnabled = model.IsTakePofitEnabled,
				TakeProfitSettings = model.TakeProfitSettings,
				IsStopLossEnabled = model.IsStopLossEnabled,
				StopLossSettings = model.StopLossSettings,
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
			bot.IsPyramidingEnabled = model.IsPyramidingEnabled;
			bot.PyramidingSettings = model.PyramidingSettings;
			bot.IsTakePofitEnabled = model.IsTakePofitEnabled;
			bot.TakeProfitSettings = model.TakeProfitSettings;
			bot.IsStopLossEnabled = model.IsStopLossEnabled;
			bot.StopLossSettings = model.StopLossSettings;
			bot.Key = model.Key;
			bot.IsEnabled = model.IsEnabled;
			bot.ModifiedDate = DateTime.Now;
			bot.LimitSettings = model.LimitSettings;
		}

		await Db.SaveChangesAsync();

		return Success("Exchange saved successfully");
	}

	[HttpPatch("{id}/enable")]
	public async Task<IActionResult> Enable(Guid id)
	{
		if (id == Guid.Empty)
			return BadRequest("Invalid exchange id");

		var bot = await Db.Bots.FirstOrDefaultAsync(x => x.Id == id);
		if (bot == null)
			return NotFound("Exchange not found");

		if (!bot.IsEnabled)
		{
			bot.IsEnabled = true;
			var managerId = await GetAvaibleBotManager();
			if (managerId == null)
			{
				await AddAudit(AuditType.UnhandledException, bot.UserId, bot.Id, $"Not found any available resource");
				return BadRequest("Not found any available resource");
			}

			bot.ManagedBy = managerId;
			await Db.SaveChangesAsync();

			return Success(null, "Exchange enabled successfully");
		}

		var isOpenPositionExists = await Db.Positions.AnyAsync(x => x.BotId == bot.Id && x.Status == PositionStatus.Open);
		if (isOpenPositionExists)
		{
			await AddAudit(AuditType.UnhandledException, bot.UserId, bot.Id, $"Cannot disable the bot; it has open position(s)");
			return BadRequest("Bot has open positions");
		}

		bot.IsEnabled = false;
		bot.ManagedBy = null;
		await Db.SaveChangesAsync();

		return Success(null, "Exchange disabled successfully");
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

		try
		{
			if (!await Cache.AcquireLock($"lock:bot:{bot.Id}:{orderRequest.Ticker}", TimeSpan.FromSeconds(10)))
			{
				await AddAudit(AuditType.OrderRequest, bot.UserId, bot.Id, $"Bot is busy");
				return BadRequest($"Bot is busy");
			}

			var exchange = await Db.Exchanges
				.AsNoTracking()
				.Include(x => x.Provider)
				.FirstAsync(x => x.Id == bot.ExchangeId);

			var symbolInfo = await ExchangeStore.GetSymbolInfo(exchange.Provider.Name, orderRequest.Ticker);
			if (symbolInfo == null)
			{
				await AddAudit(AuditType.OrderRequest, bot.UserId, bot.Id, $"Symbol not found.");
				return BadRequest($"Symbol not found.");
			}

			var position = await Db.Positions.GetExecuted(orderRequest.Ticker, orderRequest.PositionType);
			if (!orderRequest.IsClose)
			{
				if (position != null && position.Status == PositionStatus.Open && !bot.IsPositionSizeExpandable)
				{
					await AddAudit(AuditType.OpenOrderPlaced, bot.UserId, bot.Id, $"Position size is not expandable");
					return BadRequest($"Position size is not expandable");
				}

				if (position != null && position.Status == PositionStatus.Requested)
				{
					await AddAudit(AuditType.OpenOrderPlaced, bot.UserId, bot.Id, $"Position is still being processed");
					return BadRequest($"Position is still being processed");
				}

				position ??= await Db.Positions.AddRequestedPosition(bot, orderRequest.Ticker, orderRequest.Leverage, orderRequest.PositionType);
				await Db.Orders.AddOpenOrder(bot, position, orderRequest.Ticker);
				await Db.SaveChangesAsync();

				return Success(null, "Order requested successfully");
			}

			if (position == null)
			{
				await AddAudit(AuditType.OrderRequest, bot.UserId, bot.Id, $"Position not found");
				return BadRequest($"Position not found");
			}

			await Db.Orders.AddCloseOrder(position, await symbolInfo.GetLastPrice());
			await Db.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			await AddAudit(AuditType.UnhandledException, bot.UserId, bot.Id, ex.Message);
			return BadRequest(ex.Message);
		}
		finally
		{
			await Cache.ReleaseLock($"lock:bot:{bot.Id}:{orderRequest.Ticker}");
		}

		return Success(null, "Order requested successfully");
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

		var symbolInfo = await ExchangeStore.GetSymbolInfoLazy(exchange.Provider.Name, ticker.ToUpper());
		if (symbolInfo == null)
			return BadRequest($"Symbol {ticker} not found");

		var orderBook = await ExchangeStore.GetOrderBook(exchange.Provider.Name, ticker.ToUpper());
		if (orderBook == null)
			return BadRequest($"Order book for {ticker} not found");

		try
		{
			var longResult = await ExchangeProviderBase.GetEntryPoint(orderBook, PositionType.Long, bot.LimitSettings);
			var shortResult = await ExchangeProviderBase.GetEntryPoint(orderBook, PositionType.Short, bot.LimitSettings);

			return Success(new
			{
				Long = Math.Round(longResult, symbolInfo.PricePrecision),
				Short = Math.Round(shortResult, symbolInfo.PricePrecision)
			});
		}
		catch (Exception ex)
		{
			return BadRequest(ex.Message);
		}
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
			return BadRequest("Invalid bot id");

		var bot = await Db.Bots.Where(x => x.UserId == CurrentUser.Id).FirstOrDefaultAsync(x => x.Id == id);
		if (bot == null)
			return NotFound("The bot not found");

		var isOpenPositionExists = await Db.Positions.AnyAsync(x => x.BotId == bot.Id && x.Status == PositionStatus.Open);
		if (isOpenPositionExists)
			return BadRequest("The bot has open positions");

		bot.IsRemoved = true;
		await Db.SaveChangesAsync();

		return Success("The bot deleted successfully");
	}

	[NonAction]
	public async Task<Guid?> GetAvaibleBotManager()
	{
		var managers = await Db.BotManagers
			.Where(x => x.PingDate.AddMinutes(10) > DateTime.UtcNow)
			.ToListAsync();

		var manager = managers.Select(x => new
		{
			x.Id,
			Count = Db.Bots.Count(x => x.ManagedBy == x.Id)
		}).OrderBy(x => x.Count)
		.FirstOrDefault();

		if (manager == null) return null;

		return manager.Id;
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
