using Microsoft.AspNetCore.Mvc;
using Astroid.Entity;
using Astroid.Web.Models;
using Astroid.Core;
using Microsoft.EntityFrameworkCore;
using Astroid.Providers;
using Astroid.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Astroid.Core.Cache;
using Astroid.Entity.Extentions;

namespace Astroid.Web;

public class PositionsController : SecureController
{
	private readonly IServiceProvider ServiceProvider;
	private readonly ExchangeInfoStore ExchangeStore;

	public PositionsController(IServiceProvider serviceProvider, AstroidDb db, ICacheService cache, ExchangeInfoStore store, ILogger<BotsController> logger) : base(db, cache)
	{
		ServiceProvider = serviceProvider;
		ExchangeStore = store;
		Logger = logger;
	}

	[HttpPost("list")]
	public async Task<IActionResult> List([FromBody] MPViewDataList<ADPosition> model)
	{
		model ??= new MPViewDataList<ADPosition>();

		model = await Db.Positions
			.Include(x => x.Exchange)
			.ThenInclude(x => x.Provider)
			.Include(x => x.Bot)
			.Where(x => x.UserId == CurrentUser.Id)
			.AsNoTracking()
			.OrderByDescending(x => x.CreatedDate)
			.ViewDataListAsync<ADPosition>(model);

		return Success(model.ForJson(x =>
		{
			var orders = Db.Orders.Where(y => y.PositionId == x.Id).OrderBy(x => x.CreatedDate).ToList();
			return new AMPosition
			{
				Id = x.Id,
				Symbol = x.Symbol,
				AveragePrice = x.AvgEntryPrice,
				EntryPrice = x.EntryPrice,
				Quantity = x.Quantity,
				CurrentQuantity = x.CurrentQuantity,
				Leverage = x.Leverage,
				Type = x.Type,
				Status = x.Status,
				CreatedDate = x.CreatedDate,
				BotLabel = x.Bot.Label,
				Exchange = new AMExchange
				{
					Name = x.Exchange.Label,
					ProviderName = x.Exchange.Provider.Name,
					ProviderType = x.Exchange.Provider.Type
				},
				RealizedPnl = Math.Round(orders.Sum(y => y.RealizedPnl), 4),
				Orders = orders
			};
		}));
	}

	[HttpPost("list-open")]
	public async Task<IActionResult> ListOpen([FromBody] MPViewDataList<ADPosition> model)
	{
		model ??= new MPViewDataList<ADPosition>();

		model = await Db.Positions
			.Include(x => x.Exchange)
			.ThenInclude(x => x.Provider)
			.Include(x => x.Bot)
			.Where(x => x.UserId == CurrentUser.Id)
			.Where(x => x.Status == PositionStatus.Open || x.Status == PositionStatus.Requested)
			.AsNoTracking()
			.OrderByDescending(x => x.CreatedDate)
			.ViewDataListAsync<ADPosition>(model);

		return Success(model.ForJson(x =>
		{
			var orders = Db.Orders.Where(y => y.PositionId == x.Id).OrderBy(x => x.CreatedDate).ToList();
			return new AMPosition
			{
				Id = x.Id,
				Symbol = x.Symbol,
				AveragePrice = x.AvgEntryPrice,
				EntryPrice = x.EntryPrice,
				Quantity = x.Quantity,
				CurrentQuantity = x.CurrentQuantity,
				Leverage = x.Leverage,
				Type = x.Type,
				Status = x.Status,
				CreatedDate = x.CreatedDate,
				BotLabel = x.Bot.Label,
				Exchange = new AMExchange
				{
					Name = x.Exchange.Label,
					ProviderName = x.Exchange.Provider.Name,
					ProviderType = x.Exchange.Provider.Type
				},
				RealizedPnl = Math.Round(orders.Sum(y => y.RealizedPnl), 4),
				Orders = orders
			};
		}));
	}

	[HttpPost("list-history")]
	public async Task<IActionResult> ListHistory([FromBody] MPViewDataList<ADPosition> model)
	{
		model ??= new MPViewDataList<ADPosition>();

		model = await Db.Positions
			.Include(x => x.Exchange)
			.ThenInclude(x => x.Provider)
			.Include(x => x.Bot)
			.Where(x => x.UserId == CurrentUser.Id)
			.Where(x => x.Status == PositionStatus.Closed || x.Status == PositionStatus.Rejected)
			.AsNoTracking()
			.OrderByDescending(x => x.CreatedDate)
			.ViewDataListAsync<ADPosition>(model);

		return Success(model.ForJson(x =>
		{
			var orders = Db.Orders.Where(y => y.PositionId == x.Id).OrderBy(x => x.CreatedDate).ToList();
			return new AMPosition
			{
				Id = x.Id,
				Symbol = x.Symbol,
				AveragePrice = x.AvgEntryPrice,
				EntryPrice = x.EntryPrice,
				Quantity = x.Quantity,
				CurrentQuantity = x.CurrentQuantity,
				Leverage = x.Leverage,
				Type = x.Type,
				Status = x.Status,
				CreatedDate = x.CreatedDate,
				BotLabel = x.Bot.Label,
				Exchange = new AMExchange
				{
					Name = x.Exchange.Label,
					ProviderName = x.Exchange.Provider.Name,
					ProviderType = x.Exchange.Provider.Type
				},
				RealizedPnl = Math.Round(orders.Sum(y => y.RealizedPnl), 4),
			};
		}));
	}

	[HttpPost("list-open-orders")]
	public async Task<IActionResult> ListOpenOrders([FromBody] MPViewDataList<ADOrder> model)
	{
		model ??= new MPViewDataList<ADOrder>();

		model = await Db.Orders
			.Include(x => x.Exchange)
			.ThenInclude(x => x.Provider)
			.Include(x => x.Bot)
			.Where(x => x.UserId == CurrentUser.Id)
			.Where(x => x.Status == OrderStatus.Open)
			.AsNoTracking()
			.OrderByDescending(x => x.CreatedDate)
			.ViewDataListAsync<ADOrder>(model);

		return Success(model);
	}

	[HttpPost("list-trade-history")]
	public async Task<IActionResult> ListTradeHistory([FromBody] MPViewDataList<ADOrder> model)
	{
		model ??= new MPViewDataList<ADOrder>();

		model = await Db.Orders
			.Include(x => x.Exchange)
			.ThenInclude(x => x.Provider)
			.Include(x => x.Bot)
			.Where(x => x.UserId == CurrentUser.Id)
			.Where(x => x.Status == OrderStatus.Filled || x.Status == OrderStatus.Rejected)
			.AsNoTracking()
			.OrderByDescending(x => x.CreatedDate)
			.ViewDataListAsync<ADOrder>(model);

		return Success(model);
	}

	[HttpPost("close-order/{id}")]
	public async Task<IActionResult> CloseOrder(Guid id)
	{
		var order = await Db.Orders
			.Include(x => x.Position)
			.Include(x => x.Exchange)
			.ThenInclude(x => x.Provider)
			.FirstOrDefaultAsync(x => x.Id == id);

		if (order == null || order.Position == null)
			return NotFound("Order not found");

		var exchange = order.Exchange;
		var bot = order.Position.Bot;

		var request = new AMOrderRequest
		{
			Ticker = order.Position.Symbol,
			Type = order.Position.Type == PositionType.Long ? "close-long" : "close-short",
			Quantity = order.Quantity,
			Key = bot.Key
		};

		var exchanger = ExchangerFactory.Create(ServiceProvider, exchange);
		if (exchanger == null)
		{
			return BadRequest($"Exchanger type {exchange.Provider.Title} not found");
		}

		try
		{
			// TODO: trigger order by changing its trigger condition to immediate
		}
		catch (Exception ex)
		{
			LogError(ex, ex.Message);
			return Error(ex.Message);
		}
		finally
		{
			await Cache.ReleaseLock($"lock:bot:{bot.Id}:{request.Ticker}");
		}

		return Success(null, "Order executed successfully");
	}

	[HttpPost("close/{id}")]
	public async Task<IActionResult> Close(Guid id)
	{
		var position = await Db.Positions
			.Include(x => x.Exchange)
			.ThenInclude(x => x.Provider)
			.Include(x => x.Bot)
			.FirstOrDefaultAsync(x => x.Id == id);

		if (position == null)
			return NotFound("Position not found");

		var exchange = position.Exchange;
		var bot = position.Bot;

		var request = new AMOrderRequest
		{
			Ticker = position.Symbol,
			Type = position.Type == PositionType.Long ? "close-long" : "close-short",
			Key = bot.Key
		};
		var symbolInfo = await ExchangeStore.GetSymbolInfo(exchange.Provider.Name, request.Ticker);
		if (symbolInfo == null)
			return BadRequest($"Symbol info not found for {request.Ticker}");

		await Db.Orders.AddClosePositionOrder(position);
		await Db.SaveChangesAsync();

		return Success(null, "Order requested successfully");
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
			CorrelationId = "",
			CreatedDate = DateTime.UtcNow,
		};

		await Db.Audits.AddAsync(audit);
		await Db.SaveChangesAsync();
	}
}
