using Astroid.Core;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Astroid.Entity.Extentions;

public static class ContextExtentionMethods
{
	public static Task<bool> IsPositionClosing(this AstroidDb db, Guid positionId) => db.Orders.AnyAsync(x => x.PositionId == positionId && x.Status == OrderStatus.Triggered && x.ClosePosition);

	public static bool IsClosing(this ADPosition position) => position.Orders.Any(x => x.Status == OrderStatus.Triggered && x.ClosePosition);

	public static void Cancel(this ADOrder order)
	{
		order.Status = OrderStatus.Cancelled;
		order.UpdatedDate = DateTime.UtcNow;
	}

	public static void Reject(this ADOrder order)
	{
		order.Status = OrderStatus.Rejected;
		order.UpdatedDate = DateTime.UtcNow;
	}

	public static void Fill(this ADOrder order, decimal quantity)
	{
		order.Status = OrderStatus.Filled;
		order.UpdatedDate = DateTime.UtcNow;
		order.FilledQuantity = quantity;
	}

	public static void Upsert<T>(this DbSet<T> set, T entity, Func<T, bool> condition) where T : class
	{
		var e = set.FirstOrDefault(condition);
		if (e != null) return;

		set.Add(entity);
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
