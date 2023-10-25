using Astroid.Core;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Astroid.Entity.Extentions;

public static class ContextExtentionMethods
{
	public static async Task AddOrderNotification(this AstroidDb db, ADOrder order, ADBot bot)
	{
		var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == order.UserId);
		if (user == null) return;

		var notification = new ADNotification
		{
			Id = Guid.NewGuid(),
			UserId = order.UserId,
			CreatedDate = DateTime.UtcNow,
			SentDate = DateTime.MinValue,
			ExpireDate = DateTime.UtcNow.AddMinutes(5),
			Channel = user.ChannelPreference,
			Status = NotificationStatus.Pending,
			Subject = $"{(order.Status == OrderStatus.Filled ? "✅" : "❌")} {order.Symbol} - {order.TriggerType.GetDescription()} Order Executed",
			Content = $"\n🤖 Bot: {bot.Label}\n📈 Trigger Price: {order.TriggerPrice}\n💸 Quantity: {order.Quantity}\n💰 Filled Quantity: {order.FilledQuantity}",
		};

		await db.Notifications.AddAsync(notification);
	}

	public static Task<bool> IsPositionClosing(this AstroidDb db, Guid positionId) => db.Orders.AnyAsync(x => x.PositionId == positionId && x.Status == OrderStatus.Triggered && x.ClosePosition);

	public static bool IsClosing(this ADPosition position) => position.Orders.Any(x => x.Status == OrderStatus.Triggered && x.ClosePosition);

	public static Task<ADPosition?> GetExecuted(this DbSet<ADPosition> set, Guid botId, string symbol, PositionType type) => set.Include(x => x.Exchange).FirstOrDefaultAsync(x => x.BotId == botId && x.Symbol == symbol && x.Type == type && (x.Status == PositionStatus.Open || x.Status == PositionStatus.Requested));

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

	public static async Task AddClosePositionOrder(this DbSet<ADOrder> set, ADPosition position, decimal? quantity = null, PositionSizeType qtyType = PositionSizeType.Unknown, bool closePosition = true)
	{
		var order = new ADOrder
		{
			Id = Guid.NewGuid(),
			UserId = position.UserId,
			BotId = position.BotId,
			ExchangeId = position.ExchangeId,
			PositionId = position.Id,
			Symbol = position.Symbol,
			TriggerPrice = 0,
			TriggerType = OrderTriggerType.Sell,
			ConditionType = OrderConditionType.Immediate,
			Quantity = quantity ?? position.Quantity,
			QuantityType = qtyType == PositionSizeType.Unknown ? PositionSizeType.FixedInAsset : qtyType,
			ClosePosition = closePosition,
			Status = OrderStatus.Open,
			UpdatedDate = DateTime.MinValue,
			CreatedDate = DateTime.UtcNow
		};

		await set.AddAsync(order);
	}

	public static async Task AddOpenOrder(this DbSet<ADOrder> set, ADBot bot, ADPosition position, string symbol, decimal? size, PositionSizeType type, bool isPyramiding)
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
			TriggerType = isPyramiding ? OrderTriggerType.Pyramiding : OrderTriggerType.Buy,
			ConditionType = OrderConditionType.Immediate,
			Quantity = size ?? bot.PositionSize ?? 0,
			QuantityType = type == PositionSizeType.Unknown ? bot.PositionSizeType : type,
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
