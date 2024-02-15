using AsyncMemoryCache.ExpirationStrategy;
using Nito.AsyncEx;
using System;
using System.Threading.Tasks;

namespace AsyncMemoryCache;

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

	public CacheEntity<TKey, TValue> WithAbsoluteExpiration(DateTimeOffset expiryDate)
	{
		ExpirationStrategy = new AbsoluteExpirationStrategy(expiryDate);
		return this;
	}

	public CacheEntity<TKey, TValue> WithSlidingExpiration(TimeSpan slidingExpirationWindow)
	{
		ExpirationStrategy = new SlidingExpirationStrategy(slidingExpirationWindow);
		return this;
	}

	public CacheEntity<TKey, TValue> WithExpirationStrategy(IExpirationStrategy expirationStrategy)
	{
		ExpirationStrategy = expirationStrategy;
		return this;
	}
}
