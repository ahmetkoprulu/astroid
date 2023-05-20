using Astroid.Core;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Astroid.Entity.Extentions;

public static class ContextExtentionMethods
{
	public static void Upsert<T>(this DbSet<T> set, T entity, Func<T, bool> condition) where T : class
	{
		var e = set.FirstOrDefault(condition);
		if (e != null) return;

		set.Add(entity);
	}

	public static void AddAudit(this AstroidDb db, AuditType type, string description, string? correlationId = null, string? data = null)
	{
		var audit = new ADAudit
		{
			Id = Guid.NewGuid(),
			UserId = Guid.Empty,
			Type = type,
			Description = description,
			CorrelationId = correlationId,
			Data = data,
			CreatedDate = DateTime.UtcNow,
		};
		db.Audits.Add(audit);
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
