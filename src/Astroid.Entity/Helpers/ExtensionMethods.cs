using Astroid.Core;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Astroid.Entity.Extentions;

public static class ContextExtentionMethods
{
	public static Task<bool> IsPositionClosing(this AstroidDb db, Guid positionId) => db.Orders.AnyAsync(x => x.PositionId == positionId && x.Status == OrderStatus.Triggered && x.ClosePosition);

	public static bool IsClosing(this ADPosition position) => position.Orders.Any(x => x.Status == OrderStatus.Triggered && x.ClosePosition);

	public static Task<ADPosition?> GetExecuted(this DbSet<ADPosition> set, string symbol, PositionType type) => set.FirstOrDefaultAsync(x => x.Symbol == symbol && x.Type == type && (x.Status == PositionStatus.Open || x.Status == PositionStatus.Requested));

	public static void Upsert<T>(this DbSet<T> set, T entity, Func<T, bool> condition) where T : class
	{
		var e = set.FirstOrDefault(condition);
		if (e != null) return;

		set.Add(entity);
	}

	public static async Task<ADPosition> AddRequestedPosition(this DbSet<ADPosition> set, ADBot bot, string symbol, int leverage, PositionType type)
	{
		var position = new ADPosition
		{
			Id = Guid.NewGuid(),
			UserId = bot.UserId,
			BotId = bot.Id,
			ExchangeId = bot.ExchangeId,
			Symbol = symbol,
			EntryPrice = 0,
			AvgEntryPrice = 0,
			Quantity = 0,
			CurrentQuantity = 0,
			Leverage = leverage,
			Type = type,
			Status = PositionStatus.Requested,
			UpdatedDate = DateTime.MinValue,
			CreatedDate = DateTime.UtcNow
		};

		await set.AddAsync(position);

		return position;
	}

	public static async Task AddCloseOrder(this DbSet<ADOrder> set, ADPosition position, decimal price)
	{
		var order = new ADOrder
		{
			Id = Guid.NewGuid(),
			UserId = position.UserId,
			BotId = position.BotId,
			ExchangeId = position.ExchangeId,
			PositionId = position.Id,
			Symbol = position.Symbol,
			TriggerPrice = price,
			TriggerType = OrderTriggerType.Sell,
			ConditionType = OrderConditionType.Immediate,
			Quantity = position.Quantity,
			QuantityType = PositionSizeType.FixedInAsset,
			ClosePosition = true,
			Status = OrderStatus.Open,
			UpdatedDate = DateTime.MinValue,
			CreatedDate = DateTime.UtcNow
		};

		await set.AddAsync(order);
	}

	public static async Task AddOpenOrder(this DbSet<ADOrder> set, ADBot bot, ADPosition position, string symbol)
	{
		var order = new ADOrder
		{
			Id = Guid.NewGuid(),
			UserId = bot.UserId,
			BotId = bot.Id,
			ExchangeId = bot.ExchangeId,
			PositionId = position.Id,
			Symbol = symbol,
			TriggerPrice = 0,
			TriggerType = OrderTriggerType.Buy,
			ConditionType = OrderConditionType.Immediate,
			Quantity = bot.PositionSize ?? 0,
			QuantityType = bot.PositionSizeType,
			ClosePosition = false,
			Status = OrderStatus.Open,
			UpdatedDate = DateTime.MinValue,
			CreatedDate = DateTime.UtcNow,
		};

		await set.AddAsync(order);
	}

	public static async Task AddAudit(this AstroidDb db, Guid userId, Guid actorId, AuditType type, string description, Guid? targetId = null, string? correlationId = null, string? data = null)
	{
		var audit = new ADAudit
		{
			Id = Guid.NewGuid(),
			UserId = userId,
			ActorId = actorId,
			TargetId = targetId,
			Type = type,
			Description = description,
			CorrelationId = correlationId,
			Data = data,
			CreatedDate = DateTime.UtcNow,
		};
		await db.Audits.AddAsync(audit);
	}

	public static T GetAs<T>(this IEntity _, string json, bool returnDefault = false) where T : new()
	{
		try
		{
			if (string.IsNullOrEmpty(json)) return new T();
			return JsonConvert.DeserializeObject<T>(json);
		}
		catch
		{
			//ignored
		}
		return returnDefault ? default : new T();
	}

	public static string SetAs<T>(this IEntity _, T obj) where T : new() => JsonConvert.SerializeObject(obj);
}
