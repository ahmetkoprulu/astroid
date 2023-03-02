using StackExchange.Redis;
using Newtonsoft.Json;

namespace Astroid.Core.Cache;

public class RedisCache : ICacheService
{
	private readonly IDatabase _database;

	public RedisCache(IDatabase database)
	{
		_database = database;
	}

	public T? Get<T>(string key)
	{
		var redisValue = _database.StringGet(key);

		if (redisValue.HasValue)
		{
			return JsonConvert.DeserializeObject<T>(redisValue!);
		}

		return default;
	}

	public void Set<T>(string key, T value, TimeSpan expiresIn)
	{
		var serializedValue = JsonConvert.SerializeObject(value);

		_database.StringSet(key, serializedValue, expiresIn);
	}

	public void Remove(string key)
	{
		_database.KeyDelete(key);
	}

	public object? AcquireLock(string key)
	{
		throw new NotImplementedException();
	}

	public bool IsLocked(string key)
	{
		throw new NotImplementedException();
	}

	public void ReleaseLock(string key)
	{
		throw new NotImplementedException();
	}
}