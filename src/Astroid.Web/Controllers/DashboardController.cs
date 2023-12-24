using Microsoft.AspNetCore.Mvc;
using Astroid.Entity;
using Microsoft.EntityFrameworkCore;
using Astroid.Core.Cache;
using Astroid.Providers;
using Astroid.Core;

namespace Astroid.Web;

public class DashboardController : SecureController
{
	public DashboardController(AstroidDb db, ICacheService cache) : base(db, cache) { }

	[HttpGet("important-stats")]
	public IActionResult GetImportantStats()
	{
		var stats = Db.Orders
			.Where(x => x.UserId == CurrentUser.Id)
			.Where(x => x.Status == OrderStatus.Filled)
			.Where(x => x.CreatedDate > DateTime.UtcNow.AddDays(-30))
			.GroupBy(_ => 1, (_, orders) => new AMDImportantStats
			{
				MaxProfit = Math.Round(orders.Max(x => x.RealizedPnl), 2),
				MaxLoss = Math.Round(orders.Min(x => x.RealizedPnl), 2),
				WinCount = orders.Count(x => x.RealizedPnl > 0),
				LossCount = orders.Count(x => x.RealizedPnl < 0)
			});

		return Success(stats.FirstOrDefault());
	}

	[HttpGet("pnl-history")]
	public IActionResult GetPnlHistory()
	{
		var pnlHistory = new AMDPnlHistory();
		var lastPnl = Db.Orders
			.Where(x => x.UserId == CurrentUser.Id)
			.Where(x => x.Status == OrderStatus.Filled)
			.Where(x => x.CreatedDate > DateTime.UtcNow.AddDays(-30))
			.Sum(x => x.RealizedPnl);

		var PrevTotalPnl = Db.Orders
			.Where(x => x.UserId == CurrentUser.Id)
			.Where(x => x.Status == OrderStatus.Filled)
			.Where(x => x.CreatedDate > DateTime.UtcNow.AddDays(-60) && x.CreatedDate < DateTime.UtcNow.AddDays(-30))
			.Sum(x => x.RealizedPnl);

		var pnlHistoryItems = Db.Orders
			.Include(x => x.Position)
			.Include(x => x.Exchange)
			.ThenInclude(x => x.Provider)
			.Where(x => x.UserId == CurrentUser.Id)
			.Where(x => x.Status == OrderStatus.Filled)
			.OrderByDescending(x => x.CreatedDate)
			.Take(20)
			.Select(x => new AMDPnlHistoryItem
			{
				Id = x.Id,
				Symbol = x.Symbol,
				Type = x.Position.Type,
				Market = x.Exchange.Label,
				Provider = x.Exchange.Provider.Name,
				RealizedPnl = x.RealizedPnl
			})
			.ToList();

		return Success(new AMDPnlHistory { LastTotalPnl = Math.Round(lastPnl, 2), PrevTotalPnl = Math.Round(PrevTotalPnl, 2), Items = pnlHistoryItems });
	}

	[HttpGet("cumulative-profit")]
	public IActionResult GetCumulativeRealizedPnlOfLast30AndPrevious30Days()
	{
		var thirtyDaysBefore = DateTime.Today.AddDays(-30);
		var sixtyDaysBefore = DateTime.Today.AddDays(-60);

		var l30Days = Enumerable.Range(0, 30)
			.Select(x => thirtyDaysBefore.AddDays(x))
			.GroupJoin(
				Db.Positions
					.Where(x => x.UserId == CurrentUser.Id)
					.Where(x => x.Status == PositionStatus.Closed || x.Status == PositionStatus.Open)
					.Where(x => x.CreatedDate > DateTime.UtcNow.AddDays(-30)),
				x => x.Date,
				x => x.CreatedDate.Date,
				(x, y) => new AMDCumulativeRealizedPnl
				{
					Date = x,
					PositionCount = y.Count(),
					CumulativePnl = y.Sum(p => p.Orders.Where(x => x.Status == OrderStatus.Filled).Sum(x => x.RealizedPnl))
				}
			).ToList();

		var p30Days = Enumerable.Range(0, 30)
			.Select(x => sixtyDaysBefore.AddDays(x))
			.GroupJoin(
				Db.Positions
					.Where(x => x.UserId == CurrentUser.Id)
					.Where(x => x.Status == PositionStatus.Closed || x.Status == PositionStatus.Open)
					.Where(x => x.CreatedDate > DateTime.UtcNow.AddDays(-60) && x.CreatedDate < DateTime.UtcNow.AddDays(-30)),
				x => x.Date,
				x => x.CreatedDate.Date,
				(x, y) => new AMDCumulativeRealizedPnl
				{
					Date = x,
					PositionCount = y.Count(),
					CumulativePnl = y.Sum(p => p.Orders.Where(x => x.Status == OrderStatus.Filled).Sum(x => x.RealizedPnl))
				}
			).ToList();

		return Success(new { Last30Days = l30Days, Previous30Days = l30Days });
	}

	[HttpGet("position-histogram")]
	public async Task<IActionResult> GetPositionHistogramByWalletsFor7Days()
	{
		var groupedPositions = await Db.Positions
			.Include(x => x.Exchange)
				.ThenInclude(x => x.Provider)
			.Include(x => x.Orders)
			.Where(x => x.UserId == CurrentUser.Id && x.CreatedDate > DateTime.UtcNow.AddDays(-7) && x.Status == PositionStatus.Closed)
			.GroupBy(x => x.Exchange)
			.Take(5)
			.Select(x => new AMDExchangePositionHistogram
			{
				Id = x.Key.Id,
				Label = x.Key.Label,
				Provider = x.Key.Provider.Name,
				PositionCount = x.Count(),
				TradeCount = x.Sum(y => y.Orders.Where(x => x.Status == OrderStatus.Filled).Count())
			})
			.OrderByDescending(x => x.PositionCount)
			.ToListAsync();

		return Success(groupedPositions);
	}

	[HttpGet("wallet-info")]
	public async Task<IActionResult> GetWalletInfo([FromServices] ExchangerFactory exchangerFactory)
	{
		var wallets = await Db.Exchanges
			.Include(x => x.Provider)
			.Where(x => x.UserId == CurrentUser.Id && !x.IsRemoved)
			.ToListAsync();

		var walletInfos = new List<AMExchangeWallet>();
		var tasks = wallets.Select(x => AddWalletInfo(x, exchangerFactory, walletInfos));
		await Task.WhenAll(tasks);

		var orderedList = walletInfos.OrderByDescending(x => x.IsHealthy).ThenBy(x => x.Name).ToList();

		return Success(orderedList);
	}

	[NonAction]
	public async Task AddWalletInfo(ADExchange exchange, ExchangerFactory factory, IList<AMExchangeWallet> wallets)
	{
		var provider = factory.Create(exchange);
		if (provider == null) return;

		var wallet = await provider.GetAccountInfo(exchange.Id, exchange.Label);
		if (wallet == null) return;

		wallet.providerName = exchange.Provider.Name;
		wallets.Add(wallet);
	}

	public class AMDImportantStats
	{
		public decimal MaxProfit { get; set; }
		public decimal MaxLoss { get; set; }
		public int WinCount { get; set; }
		public int LossCount { get; set; }
	}

	public class AMDPnlHistory
	{
		public decimal LastTotalPnl { get; set; }
		public decimal PrevTotalPnl { get; set; }
		public List<AMDPnlHistoryItem> Items { get; set; } = new List<AMDPnlHistoryItem>();
	}

	public class AMDPnlHistoryItem
	{
		public Guid Id { get; set; }
		public string Symbol { get; set; } = string.Empty;
		public PositionType Type { get; set; }
		public string Market { get; set; } = string.Empty;
		public string Provider { get; set; } = string.Empty;
		public decimal RealizedPnl { get; set; }
	}

	public class AMDExchangePositionHistogram
	{
		public Guid Id { get; set; }
		public string Label { get; set; } = string.Empty;
		public string Provider { get; set; } = string.Empty;
		public int PositionCount { get; set; }
		public int TradeCount { get; set; }
	}

	public class AMDCumulativeRealizedPnl
	{
		public DateTime Date { get; set; }
		public int PositionCount { get; set; }
		public decimal CumulativePnl { get; set; }
	}
}
