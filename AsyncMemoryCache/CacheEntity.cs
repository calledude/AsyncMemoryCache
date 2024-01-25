using Nito.AsyncEx;
using System;
using System.Threading.Tasks;

namespace AsyncMemoryCache;

public interface ICacheEntity<TKey, TValue>
	where TKey : notnull
	where TValue : IAsyncDisposable
{
	TKey Key { get; }
	DateTimeOffset? AbsoluteExpiration { get; set; }
	TimeSpan? SlidingExpiration { get; set; }
	AsyncLazy<TValue> ObjectFactory { get; }

	ICacheEntity<TKey, TValue> WithAbsoluteExpiration(DateTimeOffset expiryDate);
	ICacheEntity<TKey, TValue> WithSlidingExpiration(TimeSpan slidingExpirationWindow);
}

public sealed class CacheEntity<TKey, TValue> : ICacheEntity<TKey, TValue>
	where TKey : notnull
	where TValue : IAsyncDisposable
{
	public CacheEntity(TKey key, Func<Task<TValue>> objectFactory, AsyncLazyFlags lazyFlags)
	{
		Key = key;
		ObjectFactory = new AsyncLazy<TValue>(objectFactory, lazyFlags);
	}

	public TKey Key { get; }
	public DateTimeOffset? AbsoluteExpiration { get; set; }
	public TimeSpan? SlidingExpiration { get; set; }
	public AsyncLazy<TValue> ObjectFactory { get; }

	internal DateTimeOffset LastUse { get; set; } = DateTimeOffset.UtcNow;
	internal DateTimeOffset Created { get; } = DateTimeOffset.UtcNow;

	public ICacheEntity<TKey, TValue> WithAbsoluteExpiration(DateTimeOffset expiryDate)
	{
		AbsoluteExpiration = expiryDate;
		return this;
	}

	public ICacheEntity<TKey, TValue> WithSlidingExpiration(TimeSpan slidingExpirationWindow)
	{
		SlidingExpiration = slidingExpirationWindow;
		return this;
	}
}
