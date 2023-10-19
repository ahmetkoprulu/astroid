namespace Astroid.Core.Cache;

public interface ICacheService
{
	Task<T?> Get<T>(string key, T defaultValue = default);
	Task<T?> GetHash<T>(string key, string field);
	Task<T> GetAllHash<T>(string key) where T : class, new();
	Task<IEnumerable<T>> GetStartsWith<T>(string key);
	Task<IEnumerable<T>> GetHashStartsWith<T>(string key) where T : class, new();

	Task Set<T>(string key, T value, TimeSpan expiresIn = default);
	Task SetBatch(List<KeyValuePair<string, string>> pairs);
	Task SetHash(string key, string field, object value);
	Task SetAllHash(string key, KeyValuePair<string, object>[] pairs);
	Task SetHashBatch(string key, List<KeyValuePair<string, string>> pairs);

	Task<bool> IsExists(string key);
	Task Remove(string key);

	Task<bool> AcquireLock(string key, TimeSpan expiresIn = default);
	Task<bool> IsLocked(string key);
	Task ReleaseLock(string key);
}
