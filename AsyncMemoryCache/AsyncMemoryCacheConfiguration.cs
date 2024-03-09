using AsyncMemoryCache.EvictionBehaviors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using static AsyncMemoryCache.EvictionBehaviors.EvictionBehavior;

namespace AsyncMemoryCache;

/// <summary>
/// An interface which configures the <see cref="AsyncMemoryCache{TKey, TValue}"/><br/>
/// Can be used to extend the configuration for use in custom implementations of either <see cref="IAsyncMemoryCache{TKey, TValue}"/> or <see cref="IEvictionBehavior"/>
/// </summary>
/// <typeparam name="TKey">The type of the key for cache items represented by the cache.</typeparam>
/// <typeparam name="TValue">The type of the value which each item in the cache will hold.</typeparam>
public interface IAsyncMemoryCacheConfiguration<TKey, TValue>
	where TKey : notnull
	where TValue : IAsyncDisposable
{
	/// <summary>
	/// The callback to be invoked when a cache item expires.
	/// </summary>
	Action<TKey, TValue>? CacheItemExpired { get; init; }

	/// <summary>
	/// The eviction behavior. Default is <see cref="DefaultEvictionBehavior"/>
	/// </summary>
	IEvictionBehavior EvictionBehavior { get; init; }

	/// <summary>
	/// The cache backing store. Default is <see cref="ConcurrentDictionary{TKey, TValue}"/>.
	/// </summary>
	IDictionary<TKey, CacheEntity<TKey, TValue>> CacheBackingStore { get; init; }
}

/// <summary>
/// The default implementation of <see cref="IAsyncMemoryCacheConfiguration{TKey, TValue}"/>
/// </summary>
/// <typeparam name="TKey">The type of the key for cache items represented by the cache.</typeparam>
/// <typeparam name="TValue">The type of the value which each item in the cache will hold.</typeparam>
public sealed class AsyncMemoryCacheConfiguration<TKey, TValue> : IAsyncMemoryCacheConfiguration<TKey, TValue>
	where TKey : notnull
	where TValue : IAsyncDisposable
{
	/// <inheritdoc/>
	public Action<TKey, TValue>? CacheItemExpired { get; init; }

	/// <inheritdoc/>
	public IEvictionBehavior EvictionBehavior { get; init; } = Default;

	/// <inheritdoc/>
	public IDictionary<TKey, CacheEntity<TKey, TValue>> CacheBackingStore { get; init; } = new ConcurrentDictionary<TKey, CacheEntity<TKey, TValue>>();
}
