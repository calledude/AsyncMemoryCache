using AsyncMemoryCache.ExpirationStrategy;
using System;

namespace AsyncMemoryCache.Extensions;

/// <summary>
/// A class containing extension methods to ease the configuration of a <see cref="CacheEntity{TKey, TValue}"/> object.
/// </summary>
public static class CacheEntityReferenceExtensions
{
	/// <summary>
	/// Helper method that sets the <see cref="CacheEntity{TKey, TValue}.ExpirationStrategy"/> to a new instance of <see cref="AbsoluteExpirationStrategy"/>.<br/>
	/// This strategy takes an absolute date into account when evaluating whether or not it is expired.<br/>
	/// Useful when more control over the lifetime of a <see cref="CacheEntity{TKey, TValue}"/> is desirable or when there are downsides to having the object alive for an indeterminate amount of time.<para/>
	/// This is a direct proxy to <see cref="CacheEntity{TKey, TValue}.WithAbsoluteExpiration(DateTimeOffset)"/>
	/// </summary>
	/// <param name="cacheEntityReference">The <see cref="CacheEntityReference{TKey, TValue}"/></param>
	/// <param name="expiryDate">The configurable exact date used when evaluating if the object should be expired.</param>
	/// <returns><see langword="this"/> to enable chained calls</returns>
	public static CacheEntityReference<TKey, TValue> WithAbsoluteExpiration<TKey, TValue>(this CacheEntityReference<TKey, TValue> cacheEntityReference, DateTimeOffset expiryDate)
		where TKey : notnull
		where TValue : IAsyncDisposable
	{
		_ = cacheEntityReference.CacheEntity.WithAbsoluteExpiration(expiryDate);
		return cacheEntityReference;
	}

	/// <summary>
	/// Helper method that sets the <see cref="CacheEntity{TKey, TValue}.ExpirationStrategy"/> to a new instance of <see cref="SlidingExpirationStrategy"/>.<br/>
	/// This strategy takes the last use of a <see cref="CacheEntity{TKey, TValue}"/> into account when evaluating whether or not it is expired.<br/>
	/// Useful when a cached object is actively used and there is no apparent downside in keeping the cached object alive for an indeterminate amount of time.<para/>
	/// This is a direct proxy to <see cref="CacheEntity{TKey, TValue}.WithSlidingExpiration(TimeSpan)"/>
	/// </summary>
	/// <param name="cacheEntityReference">The <see cref="CacheEntityReference{TKey, TValue}"/></param>
	/// <param name="slidingExpirationWindow">The configurable window used when evaluating if the object should be expired.</param>
	/// <returns><see langword="this"/> to enable chained calls</returns>
	public static CacheEntityReference<TKey, TValue> WithSlidingExpiration<TKey, TValue>(this CacheEntityReference<TKey, TValue> cacheEntityReference, TimeSpan slidingExpirationWindow)
		where TKey : notnull
		where TValue : IAsyncDisposable
	{
		_ = cacheEntityReference.CacheEntity.WithSlidingExpiration(slidingExpirationWindow);
		return cacheEntityReference;
	}

	/// <summary>
	/// Helper method that sets the <see cref="CacheEntity{TKey, TValue}.ExpirationStrategy"/> to the provided instance of a custom implementation of <see cref="IExpirationStrategy"/>.<br/>
	/// This is a direct proxy to <see cref="CacheEntity{TKey, TValue}.WithExpirationStrategy(IExpirationStrategy)"/>
	/// </summary>
	/// <param name="cacheEntityReference">The <see cref="CacheEntityReference{TKey, TValue}"/></param>
	/// <param name="expirationStrategy">The custom implementation instance of <see cref="IExpirationStrategy"/></param>
	/// <returns><see langword="this"/> to enable chained calls</returns>
	public static CacheEntityReference<TKey, TValue> WithExpirationStrategy<TKey, TValue>(this CacheEntityReference<TKey, TValue> cacheEntityReference, IExpirationStrategy expirationStrategy)
		where TKey : notnull
		where TValue : IAsyncDisposable
	{
		_ = cacheEntityReference.CacheEntity.WithExpirationStrategy(expirationStrategy);
		return cacheEntityReference;
	}

	/// <summary>
	/// Helper method that configures the <see cref="CacheEntity{TKey, TValue}.ExpirationCallback"/><br/>
	/// This is a direct proxy to <see cref="CacheEntity{TKey, TValue}.WithExpirationCallback(Action{TKey, TValue})"/>
	/// </summary>
	/// <param name="cacheEntityReference">The <see cref="CacheEntityReference{TKey, TValue}"/></param>
	/// <param name="expirationCallback">The callback which will be invoked when this instance of <see cref="CacheEntity{TKey, TValue}"/> expires.</param>
	/// <returns><see langword="this"/> to enable chained calls</returns>
	public static CacheEntityReference<TKey, TValue> WithExpirationCallback<TKey, TValue>(this CacheEntityReference<TKey, TValue> cacheEntityReference, Action<TKey, TValue> expirationCallback)
		where TKey : notnull
		where TValue : IAsyncDisposable
	{
		_ = cacheEntityReference.CacheEntity.WithExpirationCallback(expirationCallback);
		return cacheEntityReference;
	}
}
