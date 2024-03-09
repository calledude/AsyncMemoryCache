namespace AsyncMemoryCache.ExpirationStrategy;

/// <summary>
/// An interface used to provide custom implementations and rules surrounding the expiration of a <see cref="CacheEntity{TKey, TValue}"/> object.
/// </summary>
public interface IExpirationStrategy
{
	/// <summary>
	/// A method that evaluates whether or not a <see cref="CacheEntity{TKey, TValue}"/> is expired.
	/// </summary>
	/// <returns>A <see cref="bool"/> that indicates whether or not the <see cref="CacheEntity{TKey, TValue}"/> being interrogated is expired.</returns>
	bool IsExpired();

	/// <summary>
	/// A method that is called by the cache every time the <see cref="CacheEntity{TKey, TValue}"/> this expiration strategy belongs to is used.
	/// </summary>
	void CacheEntityAccessed();
}
