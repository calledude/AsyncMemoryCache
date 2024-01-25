using AsyncMemoryCache.EvictionBehaviors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using static AsyncMemoryCache.EvictionBehaviors.EvictionBehavior;

namespace AsyncMemoryCache;

public sealed class AsyncMemoryCacheConfiguration<TKey, TValue>
	where TKey : notnull
	where TValue : IAsyncDisposable
{
	public Action<TKey, TValue>? CacheItemExpired { get; init; }
	public IEvictionBehavior EvictionBehavior { get; init; } = Default;
	public IDictionary<TKey, CacheEntity<TKey, TValue>> CacheBackingStore { get; init; } = new ConcurrentDictionary<TKey, CacheEntity<TKey, TValue>>();
}
