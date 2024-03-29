using Astroid.Core;
using Astroid.Entity;
using Microsoft.EntityFrameworkCore;

public class ExecutionRepository : IRepository, IDisposable
{
	private AstroidDb Db { get; set; }

	public ExecutionRepository(AstroidDb db) => Db = db;

	public async Task<ADPosition> GetPosition(Guid id)
	{
		var position = await Db.Positions.FirstOrDefaultAsync(x => x.Id == id) ?? throw new Exception("Position not found.");

		return position;
	}

	public async Task<ADPosition?> GetPosition(Guid exchangeId, string ticker, PositionType positionType)
	{
		var position = await Db.Positions
			.Where(x => x.ExchangeId == exchangeId && x.Status == PositionStatus.Open && x.Type == positionType)
			.FirstOrDefaultAsync(x => x.Symbol == ticker);

		if (position == null) return null;

		return position;
	}

	public async Task<ADPosition> GetCurrentPosition(Guid exchangeId, string ticker, PositionType positionType)
	{
		var position = await Db.Positions
			.Include(x => x.Bot)
			.Include(x => x.Orders)
			.Where(x => x.ExchangeId == exchangeId && x.Status == PositionStatus.Open || x.Status == PositionStatus.Requested)
			.FirstOrDefaultAsync(x => x.Symbol == ticker && x.Type == positionType) ?? throw new Exception($"No open {positionType} position found for {ticker}");

		return position;
	}

	protected async Task<IEnumerable<ADPosition>> GetCurrentPositions(Guid exchangeId)
	{
		var positions = await Db.Positions
			.Where(x => x.ExchangeId == exchangeId && x.Status == PositionStatus.Open)
			.ToListAsync();

		return positions;
	}

	public async Task<ADOrder> GetOrder(Guid? id)
	{
		if (id == null) throw new ArgumentNullException("Order not found.");

		var order = await Db.Orders
			.Include(x => x.Position)
			.FirstOrDefaultAsync(x => x.Id == id) ?? throw new Exception("Order not found.");

		return order;
	}

	public async Task<ADOrder?> GetOrderOrDefault(Guid? id)
	{
		if (id == null) return null;

		var order = await Db.Orders
			.Include(x => x.Position)
			.FirstOrDefaultAsync(x => x.Id == id);

		return order;
	}

	public async Task<ADPosition> AddPosition(ADBot bot, PositionType positionType, string ticker, int leverage, decimal entryPrice, decimal quantity)
	{
		var position = new ADPosition
		{
			Id = Guid.NewGuid(),
			UserId = bot.UserId,
			BotId = bot.Id,
			ExchangeId = bot.ExchangeId,
			Symbol = ticker,
			EntryPrice = entryPrice,
			AvgEntryPrice = entryPrice,
			Leverage = leverage,
			Quantity = quantity,
			CurrentQuantity = quantity,
			Type = positionType,
			Status = PositionStatus.Open,
			UpdatedDate = DateTime.MinValue,
			CreatedDate = DateTime.UtcNow
		};

		await Db.Positions.AddAsync(position);

		return position;
	}

	public async Task ReducePosition(ADOrder order, bool success, decimal quantity, decimal pnl, bool rejectPosition)
	{
		if (!success)
		{
			if (rejectPosition) await RejectOrderWithPosition(order);
			else order.Reject();

			return;
		}

		if (order.ClosePosition) await CancelOpenOrders(order.Position, true);

		order.Fill(quantity);
		order.RealizedPnl = pnl;
		order.Position.Reduce(quantity);
	}

	public void ExpandPosition(ADOrder order, decimal quantity, decimal entryPrice, int? leverage = null)
	{
		order.Position.Expand(quantity, entryPrice);
		if (leverage.HasValue) order.Position.ChangeLeverage(leverage.Value);

		order.Fill(quantity, entryPrice);
	}

	public async Task RejectOrderWithPosition(ADOrder order)
	{
		await CancelOpenOrders(order.Position);
		order.Position.Reject();
		order.Reject();
	}

	public async Task ClosePosition(ADOrder order, decimal quantity, decimal price, decimal pnl)
	{
		order.Fill(quantity, price);
		order.RealizedPnl = pnl;
		await CancelOpenOrders(order.Position, true);
	}

	public void CloseRequestedPosition(ADPosition position, ADOrder order)
	{
		position.Close();
		order.Fill(0);
	}

	public async Task<ADOrder> AddOrder(ADPosition position, OrderTriggerType triggerType, OrderConditionType conditionType, decimal price, decimal quantity, PositionSizeType qtyType, bool closePosition, Guid? relatedTo = null)
	{
		var order = new ADOrder
		{
			Id = Guid.NewGuid(),
			UserId = position.UserId,
			BotId = position.BotId,
			ExchangeId = position.ExchangeId,
			PositionId = position.Id,
			Symbol = position.Symbol,
			TriggerType = triggerType,
			ConditionType = conditionType,
			TriggerPrice = price,
			Quantity = quantity,
			QuantityType = qtyType,
			ClosePosition = closePosition,
			Status = OrderStatus.Open,
			RelatedTo = relatedTo,
			UpdatedDate = DateTime.MinValue,
			CreatedDate = DateTime.UtcNow
		};
		await Db.Orders.AddAsync(order);

		return order;
	}

	public async Task<List<ADOrder>> GetOpenOrders(Guid exchangeId, Guid positionId, OrderTriggerType triggerType) =>
		await Db.Orders
			.Where(x => x.ExchangeId == exchangeId && x.PositionId == positionId && x.Status == OrderStatus.Open && x.TriggerType == triggerType)
			.ToListAsync();

	public async Task<ADOrder?> GetOpenStopLossOrder(Guid positionId)
	{
		var order = await Db.Orders
			.Where(x => x.PositionId == positionId && x.Status == OrderStatus.Open && x.TriggerType == OrderTriggerType.StopLoss)
			.FirstOrDefaultAsync();

		if (order == null) return null;

		return order;
	}

	public async Task CancelOpenOrders(ADPosition position, bool closePosition = false)
	{
		if (position == null) return;

		await Db.Orders.Where(x => x.PositionId == position.Id && x.Status == OrderStatus.Open)
			.ForEachAsync(x =>
			{
				x.Status = OrderStatus.Cancelled;
				x.UpdatedDate = DateTime.UtcNow;
			});

		if (closePosition) position.Close();
	}

	public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Db.SaveChangesAsync(cancellationToken);
	public void Dispose() => Db.Dispose();
}
