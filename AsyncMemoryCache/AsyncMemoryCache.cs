using AsyncMemoryCache.EvictionBehaviors;
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

/// <summary>
/// This interface exists mainly to avoid having concrete types as constructor arguments.
/// An effect of this is enabling subsitution and as a result, easier testing, where an actual concrete instance is not required in each and every test that uses a class that looks like the following
/// <para/>
/// <code>
/// public MyService(AsyncMemoryCache&lt;string, SomeCacheable&gt;) { }
/// </code>
/// </summary>
/// <typeparam name="TKey">The type of the key for cache items represented by the cache.</typeparam>
/// <typeparam name="TValue">The type of the value which each item in the cache will hold.</typeparam>
public interface IAsyncMemoryCache<TKey, TValue>
	where TKey : notnull
	where TValue : IAsyncDisposable
{
	/// <summary>
	/// Gets the cached item with the specified key.
	/// </summary>
	/// <param name="key">The key of the element to get.</param>
	/// <returns>A <see cref="CacheEntityReference{TKey, TValue}"/> representing the lifetime of the underlying <see cref="CacheEntity{TKey, TValue}"/> until disposed.</returns>
	CacheEntityReference<TKey, TValue> this[TKey key] { get; }

	/// <summary>
	/// Gets the value associated with this key if it exists, or generates a new entry using the provided key and a value from the given factory if the key is not found.<br/>
	/// The <paramref name="objectFactory"/> is started in a non-blocking fashion, and the result from it will have to be <c>await</c>ed
	/// </summary>
	/// <param name="key">The cache item key.</param>
	/// <param name="objectFactory">The object factory.</param>
	/// <param name="lazyFlags">Optional <see cref="AsyncLazyFlags"/> to configure object factory behavior.</param>
	/// <returns>A <see cref="CacheEntityReference{TKey, TValue}"/> representing the lifetime of the underlying <see cref="CacheEntity{TKey, TValue}"/> until disposed.</returns>
	CacheEntityReference<TKey, TValue> GetOrCreate(TKey key, Func<Task<TValue>> objectFactory, AsyncLazyFlags lazyFlags = AsyncLazyFlags.None);

	/// <summary>
	/// Determines whether the <see cref="IAsyncMemoryCache{TKey, TValue}"/> contains an element with the specified key.
	/// </summary>
	/// <param name="key">The key to locate in the <see cref="IAsyncMemoryCache{TKey, TValue}"/>.</param>
	/// <returns><see langword="true"/> if the <see cref="IAsyncMemoryCache{TKey, TValue}"/> contains a cache item with the key; otherwise, <see langword="false"/>.</returns>
	bool ContainsKey(TKey key);

	/// <summary>
	/// Gets the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key whose value to get.</param>
	/// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
	/// <returns><see langword="true"/> if a cache item with the specified key exists; otherwise <see langword="false"/>.</returns>
	bool TryGetValue(TKey key, [NotNullWhen(true)] out CacheEntityReference<TKey, TValue>? value);
}

/// <summary>
/// The default implementation of <see cref="IAsyncMemoryCache{TKey, TValue}"/>
/// </summary>
/// <typeparam name="TKey">The type of the key for cache items represented by the cache.</typeparam>
/// <typeparam name="TValue">The type of the value which each item in the cache will hold.</typeparam>
public sealed class AsyncMemoryCache<TKey, TValue> : IAsyncDisposable, IAsyncMemoryCache<TKey, TValue>
	where TKey : notnull
	where TValue : IAsyncDisposable
{
	private readonly IAsyncMemoryCacheConfiguration<TKey, TValue> _configuration;
	private readonly IDictionary<TKey, CacheEntity<TKey, TValue>> _cache;
	private readonly ILogger<AsyncMemoryCache<TKey, TValue>> _logger;

	public AsyncMemoryCache(IAsyncMemoryCacheConfiguration<TKey, TValue> configuration, ILogger<AsyncMemoryCache<TKey, TValue>>? logger = null)
	{
#if NET8_0_OR_GREATER
		ArgumentNullException.ThrowIfNull(configuration);
#else
		if (configuration is null)
			throw new ArgumentNullException(nameof(configuration));
#endif

		_configuration = configuration;
		_cache = configuration.CacheBackingStore;

		_logger = logger ?? NullLoggerFactory.Instance.CreateLogger<AsyncMemoryCache<TKey, TValue>>();
		configuration.EvictionBehavior.Start(configuration, _logger);
	}

	/// <inheritdoc/>
	public CacheEntityReference<TKey, TValue> this[TKey key]
	{
		get
		{
			var cacheEntity = _cache[key];
			cacheEntity.ExpirationStrategy?.CacheEntityAccessed();
			return new(cacheEntity);
		}
	}

	/// <inheritdoc/>
	public CacheEntityReference<TKey, TValue> GetOrCreate(TKey key, Func<Task<TValue>> objectFactory, AsyncLazyFlags lazyFlags = AsyncLazyFlags.None)
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

	/// <inheritdoc/>
	public bool ContainsKey(TKey key)
		=> _cache.ContainsKey(key);

	/// <inheritdoc/>
	public bool TryGetValue(TKey key, [NotNullWhen(true)] out CacheEntityReference<TKey, TValue>? value)
	{
		if (_cache.TryGetValue(key, out var entity))
		{
			// If this if-statement fails it means that the refcount was at least -1
			// so it is in fact expired, as such we shouldn't use it
			if (Interlocked.Increment(ref entity.References) > 0)
			{
				entity.ExpirationStrategy?.CacheEntityAccessed();

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
