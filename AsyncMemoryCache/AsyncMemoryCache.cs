using AsyncMemoryCache.EvictionBehaviors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncMemoryCache;

public interface IAsyncMemoryCache<T> where T : IAsyncDisposable
{
	AsyncLazy<T> this[string key] { get; }
	ICacheEntity<T> Add(string key, Func<Task<T>> objectFactory, AsyncLazyFlags lazyFlags = AsyncLazyFlags.None);
	bool ContainsKey(string key);
}

public sealed class AsyncMemoryCache<T> : IAsyncDisposable, IAsyncMemoryCache<T> where T : IAsyncDisposable
{
	private readonly IDictionary<string, CacheEntity<T>> _cache;
	private readonly IEvictionBehavior _evictionBehavior;
	private readonly ILogger<AsyncMemoryCache<T>> _logger;

	public AsyncMemoryCache(AsyncMemoryCacheConfiguration<T> configuration, ILogger<AsyncMemoryCache<T>>? logger = null)
	{
		_cache = configuration.CacheBackingStore;

		_logger = logger ?? NullLoggerFactory.Instance.CreateLogger<AsyncMemoryCache<T>>();
		_evictionBehavior = configuration.EvictionBehavior;
		_evictionBehavior.Start(configuration, _logger);
	}

	public AsyncLazy<T> this[string key] => _cache[key].ObjectFactory;

	public ICacheEntity<T> Add(string key, Func<Task<T>> objectFactory, AsyncLazyFlags lazyFlags = AsyncLazyFlags.None)
	{
		_logger.LogTrace("Adding item with key: {key}", key);
		if (_cache.TryGetValue(key, out var entity))
			return entity;

		var cacheEntity = new CacheEntity<T>(key, objectFactory, lazyFlags);
		cacheEntity.ObjectFactory.Start();
		_cache[key] = cacheEntity;

		_logger.LogTrace("Added item with key: {key}", key);
		return cacheEntity;
	}

	public bool ContainsKey(string key)
		=> _cache.ContainsKey(key);

	public async ValueTask DisposeAsync()
		=> await _evictionBehavior.DisposeAsync();
}
