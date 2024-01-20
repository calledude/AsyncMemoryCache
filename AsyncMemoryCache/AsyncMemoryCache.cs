using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace AsyncMemoryCache;

public sealed class AsyncMemoryCache<T> : IAsyncDisposable where T : IAsyncDisposable
{
	internal readonly ConcurrentDictionary<string, CacheEntity<T>> Cache;
	private readonly EvictionBehavior _evictionBehavior;

	public Action<string, T>? CacheItemExpired { get; init; }

	public AsyncMemoryCache(EvictionBehavior? evictionBehavior = null)
	{
		Cache = [];
		_evictionBehavior = evictionBehavior ?? EvictionBehavior.Default;

		var weakRef = new WeakReference<AsyncMemoryCache<T>>(this);
		_evictionBehavior.Start(weakRef);
	}

	public AsyncLazy<T> this[string key] => Cache[key].ObjectFactory;

	public CacheEntity<T> Add(string key, Func<Task<T>> objectFactory)
	{
		if (Cache.TryGetValue(key, out var entity))
			return entity;

		var cacheEntity = new CacheEntity<T>(key, objectFactory);
		cacheEntity.ObjectFactory.Start();
		Cache[key] = cacheEntity;

		return cacheEntity;
	}

	public bool ContainsKey(string key) => Cache.ContainsKey(key);

	internal void InvokeCacheItemExpiredEvent(string key, T item) => CacheItemExpired?.Invoke(key, item);

	public async ValueTask DisposeAsync()
	{
		await _evictionBehavior.DisposeAsync();
	}
}
