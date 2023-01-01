using Microsoft.EntityFrameworkCore;

namespace Astroid.Entity.Extentions;

public static class ContextExtentionMethods
{
	public static void Upsert<T>(this DbSet<T> set, T entity, Func<T, bool> condition) where T : class
	{
		var e = set.FirstOrDefault(condition);
		if (e != null) return;

		set.Add(entity);
	}
}