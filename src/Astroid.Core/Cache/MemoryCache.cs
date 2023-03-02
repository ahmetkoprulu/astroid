using Microsoft.Extensions.Caching.Memory;

namespace Astroid.Core.Cache;

public class InMemoryCache : ICacheService
{
	private readonly IMemoryCache _cache;
	private static readonly Dictionary<string, object> Locks = new();

	public InMemoryCache(IMemoryCache cache)
	{
		_cache = cache;
	}

	public T? Get<T>(string key) => _cache.Get<T>(key);

	public void Set<T>(string key, T value, TimeSpan expiresIn)
	{
		var options = new MemoryCacheEntryOptions
		{
			AbsoluteExpirationRelativeToNow = expiresIn
		};

		_cache.Set(key, value, options);
	}

	public void Remove(string key)
	{
		_cache.Remove(key);
	}

	public void RemoveAll()
	{
		_cache.Dispose();
	}

	public object? AcquireLock(string key)
	{
		lock (Locks)
		{
			var exist = Locks.TryGetValue(key, out var @lock);
			if (exist) return @lock;

			@lock = new object();
			Locks.Add(key, @lock);

			return @lock;
		}
	}

	public bool IsLocked(string key)
	{
		lock (Locks)
		{
			return Locks.ContainsKey(key);
		}
	}

	public void ReleaseLock(string key)
	{
		lock (Locks)
		{
			Locks.Remove(key);
		}
	}
}