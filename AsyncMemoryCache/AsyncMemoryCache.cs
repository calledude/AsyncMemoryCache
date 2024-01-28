using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
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
	private readonly IAsyncMemoryCacheConfiguration<TKey, TValue> _configuration;
	private readonly IDictionary<TKey, CacheEntity<TKey, TValue>> _cache;
	private readonly ILogger<AsyncMemoryCache<TKey, TValue>> _logger;

	public AsyncMemoryCache(IAsyncMemoryCacheConfiguration<TKey, TValue> configuration, ILogger<AsyncMemoryCache<TKey, TValue>>? logger = null)
	{
		_configuration = configuration;
		_cache = configuration.CacheBackingStore;

		_logger = logger ?? NullLoggerFactory.Instance.CreateLogger<AsyncMemoryCache<TKey, TValue>>();
		configuration.EvictionBehavior.Start(configuration, _logger);
	}

	public AsyncLazy<TValue> this[TKey key]
	{
		get
		{
			var cacheEntity = _cache[key];
			cacheEntity.LastUse = DateTimeOffset.UtcNow;
			return cacheEntity.ObjectFactory;
		}
	}

	public ICacheEntity<TKey, TValue> Add(TKey key, Func<Task<TValue>> objectFactory, AsyncLazyFlags lazyFlags = AsyncLazyFlags.None)
	{
		_logger.LogTrace("Adding item with key: {key}", key);
		if (_cache.TryGetValue(key, out var entity))
		{
			entity.LastUse = DateTimeOffset.UtcNow;
			return entity;
		}

		var cacheEntity = new CacheEntity<TKey, TValue>(key, objectFactory, lazyFlags);
		cacheEntity.ObjectFactory.Start();
		_cache[key] = cacheEntity;

		_logger.LogTrace("Added item with key: {key}", key);
		return cacheEntity;
	}

	public bool ContainsKey(TKey key)
		=> _cache.ContainsKey(key);

	public async ValueTask DisposeAsync()
	{
		await _configuration.EvictionBehavior.DisposeAsync();

		var diposeTasks = _configuration.CacheBackingStore
			.Select(async x =>
			{
				var cachedObject = await x.Value.ObjectFactory;
				await cachedObject.DisposeAsync();
			});

		await Task.WhenAll(diposeTasks);
	}
}
