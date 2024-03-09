using AsyncMemoryCache.ExpirationStrategy;
using Nito.AsyncEx;
using System;
using System.Threading.Tasks;

namespace AsyncMemoryCache;

/// <summary>
/// Wraps the cached object together with its configured expiration strategy and callbacks.
/// </summary>
/// <typeparam name="TKey">The type of the key used for the item.</typeparam>
/// <typeparam name="TValue">The type of the value which the <see cref="CacheEntity{TKey, TValue}"/> wraps.</typeparam>
public sealed class CacheEntity<TKey, TValue>
	where TKey : notnull
	where TValue : IAsyncDisposable
{
	public CacheEntity(TKey key, Func<Task<TValue>> objectFactory, AsyncLazyFlags lazyFlags)
	{
		Key = key;
		ObjectFactory = new(objectFactory, lazyFlags);
	}

	public TKey Key { get; }
	public AsyncLazy<TValue> ObjectFactory { get; }

	private int _references;
	internal ref int References => ref _references;
	internal IExpirationStrategy? ExpirationStrategy { get; private set; }
	internal Action<TKey, TValue>? ExpirationCallback { get; private set; }

	/// <summary>
	/// Sets the <see cref="ExpirationStrategy"/> to a new instance of <see cref="AbsoluteExpirationStrategy"/>.<br/>
	/// This strategy takes an absolute date into account when evaluating whether or not it is expired.<br/>
	/// Useful when more control over the lifetime of a <see cref="CacheEntity{TKey, TValue}"/> is desirable or when there are downsides to having the object alive for an indeterminate amount of time.<br/>
	/// </summary>
	/// <param name="expiryDate">The configurable exact date used when evaluating if the object should be expired.</param>
	/// <returns><see langword="this"/> to enable chained calls</returns>
	public CacheEntity<TKey, TValue> WithAbsoluteExpiration(DateTimeOffset expiryDate)
	{
		ExpirationStrategy = new AbsoluteExpirationStrategy(expiryDate);
		return this;
	}

	/// <summary>
	/// Sets the <see cref="ExpirationStrategy"/> to a new instance of <see cref="SlidingExpirationStrategy"/>.<br/>
	/// This strategy takes the last use of a <see cref="CacheEntity{TKey, TValue}"/> into account when evaluating whether or not it is expired.<br/>
	/// Useful when a cached object is actively used and there is no apparent downside in keeping the cached object alive for an indeterminate amount of time.<br/>
	/// </summary>
	/// <param name="slidingExpirationWindow">The configurable window used when evaluating if the object should be expired.</param>
	/// <returns><see langword="this"/> to enable chained calls</returns>
	public CacheEntity<TKey, TValue> WithSlidingExpiration(TimeSpan slidingExpirationWindow)
	{
		ExpirationStrategy = new SlidingExpirationStrategy(slidingExpirationWindow);
		return this;
	}

	/// <summary>
	/// Sets the <see cref="ExpirationStrategy"/> to the provided instance of a custom implementation of <see cref="IExpirationStrategy"/>.<br/>
	/// </summary>
	/// <param name="expirationStrategy">The custom implementation instance of <see cref="IExpirationStrategy"/></param>
	/// <returns><see langword="this"/> to enable chained calls</returns>
	public CacheEntity<TKey, TValue> WithExpirationStrategy(IExpirationStrategy expirationStrategy)
	{
		ExpirationStrategy = expirationStrategy;
		return this;
	}

	/// <summary>
	/// Configures the <see cref="ExpirationCallback"/><br/>
	/// </summary>
	/// <param name="expirationCallback">The callback which will be invoked when this instance of <see cref="CacheEntity{TKey, TValue}"/> expires.</param>
	/// <returns><see langword="this"/> to enable chained calls</returns>
	public CacheEntity<TKey, TValue> WithExpirationCallback(Action<TKey, TValue> expirationCallback)
	{
		ExpirationCallback = expirationCallback;
		return this;
	}
}
