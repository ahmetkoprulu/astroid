namespace Astroid.Core.Cache;

public interface ICacheService
{
	T? Get<T>(string key);
	void Set<T>(string key, T value, TimeSpan expiresIn);
	void Remove(string key);
	object? AcquireLock(string key);
	bool IsLocked(string key);
	void ReleaseLock(string key);
}