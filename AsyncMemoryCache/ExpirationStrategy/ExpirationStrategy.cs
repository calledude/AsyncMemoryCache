namespace AsyncMemoryCache.ExpirationStrategy;

public interface IExpirationStrategy
{
	bool IsExpired();
	void CacheEntityAccessed();
}
