using AsyncMemoryCache.ExpirationStrategy;
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

	public static CacheEntityReference<TKey, TValue> WithExpirationStrategy<TKey, TValue>(this CacheEntityReference<TKey, TValue> cacheEntityReference, IExpirationStrategy expirationStrategy)
		where TKey : notnull
		where TValue : IAsyncDisposable
	{
		_ = cacheEntityReference.CacheEntity.WithExpirationStrategy(expirationStrategy);
		return cacheEntityReference;
	}

	public static CacheEntityReference<TKey, TValue> WithExpirationCallback<TKey, TValue>(this CacheEntityReference<TKey, TValue> cacheEntityReference, Action<TKey, TValue> expirationCallback)
		where TKey : notnull
		where TValue : IAsyncDisposable
	{
		_ = cacheEntityReference.CacheEntity.WithExpirationCallback(expirationCallback);
		return cacheEntityReference;
	}
}
