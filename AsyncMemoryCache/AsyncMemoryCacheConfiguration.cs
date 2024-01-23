using AsyncMemoryCache.EvictionBehaviors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using static AsyncMemoryCache.EvictionBehaviors.EvictionBehavior;

namespace AsyncMemoryCache;

public sealed class AsyncMemoryCacheConfiguration<T> where T : IAsyncDisposable
{
	public Action<string, T>? CacheItemExpired { get; init; }
	public IEvictionBehavior EvictionBehavior { get; init; } = Default;
	public IDictionary<string, CacheEntity<T>> CacheBackingStore { get; init; } = new ConcurrentDictionary<string, CacheEntity<T>>();
}
