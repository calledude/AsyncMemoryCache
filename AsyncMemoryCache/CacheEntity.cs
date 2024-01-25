using Nito.AsyncEx;
using System;
using System.Threading.Tasks;

namespace AsyncMemoryCache;

public interface ICacheEntity<TKey, TValue>
	where TKey : notnull
	where TValue : IAsyncDisposable
{
	TKey Key { get; }
	TimeSpan Lifetime { get; set; }
	AsyncLazy<TValue> ObjectFactory { get; }
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

	internal DateTime Created { get; } = DateTime.UtcNow;

	public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(30);
	public TKey Key { get; }
	public AsyncLazy<TValue> ObjectFactory { get; }
}
