using Microsoft.Extensions.Caching.Memory;

namespace Astroid.Core.Cache;

public class InMemoryCache : ICacheService
{
	private readonly IMemoryCache _cache;
	private static readonly Dictionary<string, object> Locks = new();

	public InMemoryCache(IMemoryCache cache) => _cache = cache;

	public async Task<T?> Get<T>(string key, T defaultValue = default)
	{
		var val = _cache.Get<T>(key);
		if (val == null) return defaultValue;

		return val;
	}

	public async Task Set<T>(string key, T value, TimeSpan expiresIn)
	{
		var options = new MemoryCacheEntryOptions
		{
			AbsoluteExpirationRelativeToNow = expiresIn
		};

		_cache.Set(key, value, options);
	}

	public async Task Remove(string key) => _cache.Remove(key);

	public async Task RemoveAll() => _cache.Dispose();

	public async Task<bool> AcquireLock(string key, TimeSpan _ = default)
	{
		lock (Locks)
		{
			var exist = Locks.TryGetValue(key, out var @lock);
			if (exist) return false;

			@lock = new object();
			Locks.Add(key, @lock);

			return true;
		}
	}

	public async Task<bool> IsLocked(string key)
	{
		lock (Locks)
		{
			return Locks.ContainsKey(key);
		}
	}

	public async Task ReleaseLock(string key)
	{
		lock (Locks)
		{
			Locks.Remove(key);
		}
	}

	public Task<IEnumerable<T>> GetStartsWith<T>(string key) => throw new NotImplementedException();
	public Task SetBatch(List<KeyValuePair<string, string>> pairs) => throw new NotImplementedException();
	public Task<T?> GetHash<T>(string key, string field) => throw new NotImplementedException();
	public Task SetHash(string key, string field, object value) => throw new NotImplementedException();
	public Task SetAllHash(string key, KeyValuePair<string, object>[] pairs) => throw new NotImplementedException();
	public Task SetHashBatch(string key, List<KeyValuePair<string, string>> pairs) => throw new NotImplementedException();
	Task<T> ICacheService.GetAllHash<T>(string key) => throw new NotImplementedException();
	public Task<bool> IsExists(string key) => throw new NotImplementedException();
	public Task<IEnumerable<T>> GetHashStartsWith<T>(string key) where T : class, new() => throw new NotImplementedException();
}
