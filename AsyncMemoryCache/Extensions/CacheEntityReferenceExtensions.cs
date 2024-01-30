using System;

namespace AsyncMemoryCache.Extensions;

public static class CacheEntityReferenceExtensions
{
	public static CacheEntityReference<TKey, TValue> WithAbsoluteExpiration<TKey, TValue>(this CacheEntityReference<TKey, TValue> cacheEntityReference, DateTimeOffset expiryDate)
		where TKey : notnull
		where TValue : IAsyncDisposable
	{
		_ = cacheEntityReference.CacheEntity.WithAbsoluteExpiration(expiryDate);
		return cacheEntityReference;
	}

	public static CacheEntityReference<TKey, TValue> WithSlidingExpiration<TKey, TValue>(this CacheEntityReference<TKey, TValue> cacheEntityReference, TimeSpan slidingExpirationWindow)
	where TKey : notnull
	where TValue : IAsyncDisposable
	{
		_ = cacheEntityReference.CacheEntity.WithSlidingExpiration(slidingExpirationWindow);
		return cacheEntityReference;
	}
}
