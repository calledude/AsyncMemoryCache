using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncMemoryCache;

public interface IAsyncMemoryCache<TKey, TValue>
	where TKey : notnull
	where TValue : IAsyncDisposable
{
	CacheEntityReference<TKey, TValue> this[TKey key] { get; }
	CacheEntityReference<TKey, TValue> Add(TKey key, Func<Task<TValue>> objectFactory, AsyncLazyFlags lazyFlags = AsyncLazyFlags.None);
	bool ContainsKey(TKey key);
	bool TryGetValue(TKey key, [NotNullWhen(true)] out CacheEntityReference<TKey, TValue>? value);
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
		ArgumentNullException.ThrowIfNull(configuration);

		_configuration = configuration;
		_cache = configuration.CacheBackingStore;

		_logger = logger ?? NullLoggerFactory.Instance.CreateLogger<AsyncMemoryCache<TKey, TValue>>();
		configuration.EvictionBehavior.Start(configuration, _logger);
	}

	public CacheEntityReference<TKey, TValue> this[TKey key]
	{
		get
		{
			var cacheEntity = _cache[key];
			cacheEntity.LastUse = DateTimeOffset.UtcNow;
			return new(cacheEntity);
		}
	}

	public CacheEntityReference<TKey, TValue> Add(TKey key, Func<Task<TValue>> objectFactory, AsyncLazyFlags lazyFlags = AsyncLazyFlags.None)
	{
		_logger.LogTrace("Adding item with key: {Key}", key);

		if (TryGetValue(key, out var entity))
			return entity;

		var cacheEntity = new CacheEntity<TKey, TValue>(key, objectFactory, lazyFlags);
		cacheEntity.ObjectFactory.Start();
		_cache[key] = cacheEntity;

		_logger.LogTrace("Added item with key: {Key}", key);
		return new(cacheEntity);
	}

	public bool ContainsKey(TKey key)
		=> _cache.ContainsKey(key);

	public bool TryGetValue(TKey key, [NotNullWhen(true)] out CacheEntityReference<TKey, TValue>? value)
	{
		if (_cache.TryGetValue(key, out var entity))
		{
			// If this if-statement fails it means that the refcount was at least -1
			// so it is in fact expired, as such we shouldn't use it
			if (Interlocked.Increment(ref entity.References) > 0)
			{
				entity.LastUse = DateTimeOffset.UtcNow;

				var cacheEntityReference = new CacheEntityReference<TKey, TValue>(entity);

				// Need to decrement here to revert the Increment in the if-statement
				_ = Interlocked.Decrement(ref entity.References);

				value = cacheEntityReference;
				return true;
			}

			// If the if-statement fails we need to decrement it again
			_ = Interlocked.Decrement(ref entity.References);
		}

		value = null;
		return false;
	}

	public async ValueTask DisposeAsync()
	{
		await _configuration.EvictionBehavior.DisposeAsync().ConfigureAwait(false);

		var diposeTasks = _configuration.CacheBackingStore
			.Select(async x =>
			{
				var cachedObject = await x.Value.ObjectFactory;
				await cachedObject.DisposeAsync().ConfigureAwait(false);
			});

		await Task.WhenAll(diposeTasks).ConfigureAwait(false);
	}
}
