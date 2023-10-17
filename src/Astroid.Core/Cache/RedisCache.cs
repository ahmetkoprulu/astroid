using StackExchange.Redis;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace Astroid.Core.Cache;


public class RedisCache : ICacheService
{
	private string ConnectionString { get; set; }  // "localhost:6379,user=root,password=1234,allowAdmin=true"
	private readonly ConnectionMultiplexer _redisConnection;

	public RedisCache(IConfiguration config)
	{
		var settings = config.Get<WebConfig>() ?? new();
		var connStringEnvVariable = Environment.GetEnvironmentVariable("ASTROID_CACHE_CONNECTION_STRING");
		ConnectionString = connStringEnvVariable ?? settings.Cache.ConnectionString;
		_redisConnection = ConnectionMultiplexer.Connect(ConnectionString ?? throw new NullReferenceException("Invalid Redis Connection String"));
	}

	public async Task<T?> Get<T>(string key, T defaultValue = default)
	{
		var database = _redisConnection.GetDatabase(0);
		var redisValue = await database.StringGetAsync(key);
		if (redisValue.IsNullOrEmpty) return defaultValue;

		return JsonConvert.DeserializeObject<T>(redisValue.ToString());
	}

	public async Task Set<T>(string key, T value, TimeSpan expiresIn = default)
	{
		var database = _redisConnection.GetDatabase(0);
		var serializedValue = JsonConvert.SerializeObject(value);
		if (expiresIn == default) await database.StringSetAsync(key, serializedValue);
		else await database.StringSetAsync(key, serializedValue, expiresIn);
	}

	public async Task Remove(string key)
	{
		var database = _redisConnection.GetDatabase(0);
		await database.KeyDeleteAsync(key);
	}


	public async Task<IEnumerable<T>> GetStartsWith<T>(string key)
	{
		var database = _redisConnection.GetDatabase(0);
		var pattern = $"{key}*";
		var keys = new List<string>();

		foreach (var endpoint in _redisConnection.GetEndPoints())
		{
			var server = _redisConnection.GetServer(endpoint);
			var ks = server.Keys(pattern: pattern);
			keys.AddRange(ks.Select(k => k.ToString()));
		}

		var values = (await database.StringGetAsync(keys.Select(k => (RedisKey)k).ToArray()))
			.Where(v => !v.IsNullOrEmpty)
			.Select(v => JsonConvert.DeserializeObject<T>(v!));

		return values;
	}

	public async Task BatchSet(List<KeyValuePair<string, string>> pairs)
	{
		var database = _redisConnection.GetDatabase(0);
		var chunks = pairs.Chunk(30);

		foreach (var c in chunks)
		{
			var tasks = c.Select(x => database.StringSetAsync(x.Key, x.Value, flags: CommandFlags.FireAndForget));
			await Task.WhenAll(tasks);
		}
	}

	public async Task<bool> AcquireLock(string key, TimeSpan expiresIn = default)
	{
		var database = _redisConnection.GetDatabase();
		var lockAcquired = await database.LockTakeAsync(key, "lockValue", expiresIn);
		return lockAcquired;
	}

	public async Task<bool> IsLocked(string key)
	{
		var database = _redisConnection.GetDatabase();
		var lockValue = await database.LockQueryAsync(key);
		return lockValue.HasValue;
	}

	public async Task ReleaseLock(string key)
	{
		var database = _redisConnection.GetDatabase();
		await database.LockReleaseAsync(key, "lockValue");
	}
}
