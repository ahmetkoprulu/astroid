namespace Astroid.Core.Cache;

public interface ICacheService
{
	Task<T?> Get<T>(string key, T defaultValue = default);
	Task<IEnumerable<T>> GetStartsWith<T>(string key);
	Task Set<T>(string key, T value, TimeSpan expiresIn = default);
	Task BatchSet(List<KeyValuePair<string, string>> pairs);
	Task Remove(string key);
	Task<bool> AcquireLock(string key, TimeSpan expiresIn = default);
	Task<bool> IsLocked(string key);
	Task ReleaseLock(string key);
}
