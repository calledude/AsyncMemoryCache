using AsyncMemoryCache.EvictionBehaviors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using static AsyncMemoryCache.EvictionBehaviors.EvictionBehavior;

namespace AsyncMemoryCache;

public interface IAsyncMemoryCacheConfiguration<TKey, TValue>
	where TKey : notnull
	where TValue : IAsyncDisposable
{
	Action<TKey, TValue>? CacheItemExpired { get; init; }
	IEvictionBehavior EvictionBehavior { get; init; }
	IDictionary<TKey, CacheEntity<TKey, TValue>> CacheBackingStore { get; init; }
}

public sealed class AsyncMemoryCacheConfiguration<TKey, TValue> : IAsyncMemoryCacheConfiguration<TKey, TValue>
	where TKey : notnull
	where TValue : IAsyncDisposable
{
	public Action<TKey, TValue>? CacheItemExpired { get; init; }
	public IEvictionBehavior EvictionBehavior { get; init; } = Default;
	public IDictionary<TKey, CacheEntity<TKey, TValue>> CacheBackingStore { get; init; } = new ConcurrentDictionary<TKey, CacheEntity<TKey, TValue>>();
}
