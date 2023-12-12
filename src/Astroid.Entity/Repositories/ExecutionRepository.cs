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

	public async Task ReducePosition(ADPosition position, bool success, decimal quantity, ADOrder? order = null)
	{
		if (order != null && !success)
		{
			order?.Reject();
			return;
		}

		if (order == null)
		{
			await CancelOpenOrders(position);
			position.Close();
			return;
		}

		if (order.ClosePosition)
		{
			await CancelOpenOrders(position);
			position.Close();
		}

		order.Fill(quantity);
		position.Reduce(quantity);
	}

	public void ExpandPosition(ADPosition position, ADOrder order, decimal quantity, decimal entryPrice, int leverage)
	{
		position.Expand(quantity, entryPrice, leverage);
		order.Fill(quantity);
	}

	public void RejectPositionWithOrder(ADPosition position, ADOrder order)
	{
		position.Close();
		order.Reject();
	}

	public async Task ClosePosition(Guid exchangeId, PositionType positionType, string ticker)
	{
		var position = await Db.Positions
			.Where(x => x.ExchangeId == exchangeId && x.Status == PositionStatus.Open && x.Type == positionType)
			.FirstOrDefaultAsync(x => x.Symbol == ticker);

		if (position == null) return;

		position.Close();
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
