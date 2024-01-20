﻿using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace AsyncMemoryCache;

public interface IAsyncMemoryCache<T> where T : IAsyncDisposable
{
	AsyncLazy<T> this[string key] { get; }

	Action<string, T>? CacheItemExpired { get; init; }

	ICacheEntity<T> Add(string key, Func<Task<T>> objectFactory, AsyncLazyFlags lazyFlags = AsyncLazyFlags.None);
	bool ContainsKey(string key);
	ValueTask DisposeAsync();
}

public sealed class AsyncMemoryCache<T> : IAsyncDisposable, IAsyncMemoryCache<T> where T : IAsyncDisposable
{
	public Action<string, T>? CacheItemExpired { get; init; }

	internal ConcurrentDictionary<string, CacheEntity<T>> Cache { get; }

	private readonly IEvictionBehavior _evictionBehavior;

	public AsyncMemoryCache(AsyncMemoryCacheConfiguration<T> configuration)
	{
		Cache = [];

		_evictionBehavior = configuration.EvictionBehavior;
		CacheItemExpired = configuration.CacheItemExpired;

		var weakRef = new WeakReference<AsyncMemoryCache<T>>(this);
		_evictionBehavior.Start(weakRef);
	}

	public AsyncLazy<T> this[string key] => Cache[key].ObjectFactory;

	public ICacheEntity<T> Add(string key, Func<Task<T>> objectFactory, AsyncLazyFlags lazyFlags = AsyncLazyFlags.None)
	{
		if (Cache.TryGetValue(key, out var entity))
			return entity;

		var cacheEntity = new CacheEntity<T>(key, objectFactory, lazyFlags);
		cacheEntity.ObjectFactory.Start();
		Cache[key] = cacheEntity;

		return cacheEntity;
	}

	public bool ContainsKey(string key)
		=> Cache.ContainsKey(key);

	internal void InvokeCacheItemExpiredEvent(string key, T item)
		=> CacheItemExpired?.Invoke(key, item);

	public async ValueTask DisposeAsync()
		=> await _evictionBehavior.DisposeAsync();
}
