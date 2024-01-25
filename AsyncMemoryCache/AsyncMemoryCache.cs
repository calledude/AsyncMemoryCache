﻿using AsyncMemoryCache.EvictionBehaviors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncMemoryCache;

public interface IAsyncMemoryCache<TKey, TValue>
	where TKey : notnull
	where TValue : IAsyncDisposable
{
	AsyncLazy<TValue> this[TKey key] { get; }
	ICacheEntity<TKey, TValue> Add(TKey key, Func<Task<TValue>> objectFactory, AsyncLazyFlags lazyFlags = AsyncLazyFlags.None);
	bool ContainsKey(TKey key);
}

public sealed class AsyncMemoryCache<TKey, TValue> : IAsyncDisposable, IAsyncMemoryCache<TKey, TValue>
	where TKey : notnull
	where TValue : IAsyncDisposable
{
	private readonly IDictionary<TKey, CacheEntity<TKey, TValue>> _cache;
	private readonly IEvictionBehavior _evictionBehavior;
	private readonly ILogger<AsyncMemoryCache<TKey, TValue>> _logger;

	public AsyncMemoryCache(AsyncMemoryCacheConfiguration<TKey, TValue> configuration, ILogger<AsyncMemoryCache<TKey, TValue>>? logger = null)
	{
		_cache = configuration.CacheBackingStore;

		_logger = logger ?? NullLoggerFactory.Instance.CreateLogger<AsyncMemoryCache<TKey, TValue>>();
		_evictionBehavior = configuration.EvictionBehavior;
		_evictionBehavior.Start(configuration, _logger);
	}

	public AsyncLazy<TValue> this[TKey key] => _cache[key].ObjectFactory;

	public ICacheEntity<TKey, TValue> Add(TKey key, Func<Task<TValue>> objectFactory, AsyncLazyFlags lazyFlags = AsyncLazyFlags.None)
	{
		_logger.LogTrace("Adding item with key: {key}", key);
		if (_cache.TryGetValue(key, out var entity))
			return entity;

		var cacheEntity = new CacheEntity<TKey, TValue>(key, objectFactory, lazyFlags);
		cacheEntity.ObjectFactory.Start();
		_cache[key] = cacheEntity;

		_logger.LogTrace("Added item with key: {key}", key);
		return cacheEntity;
	}

	public bool ContainsKey(TKey key)
		=> _cache.ContainsKey(key);

	public async ValueTask DisposeAsync()
		=> await _evictionBehavior.DisposeAsync();
}
