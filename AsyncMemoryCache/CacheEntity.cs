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
		ObjectFactory = new AsyncLazy<TValue>(objectFactory, lazyFlags);
	}

	public TKey Key { get; }
	public DateTimeOffset? AbsoluteExpiration { get; set; }
	public TimeSpan? SlidingExpiration { get; set; }
	public AsyncLazy<TValue> ObjectFactory { get; }

	internal DateTimeOffset LastUse { get; set; } = DateTimeOffset.UtcNow;

	public CacheEntity<TKey, TValue> WithAbsoluteExpiration(DateTimeOffset expiryDate)
	{
		AbsoluteExpiration = expiryDate;
		return this;
	}

	public CacheEntity<TKey, TValue> WithSlidingExpiration(TimeSpan slidingExpirationWindow)
	{
		SlidingExpiration = slidingExpirationWindow;
		return this;
	}
}
