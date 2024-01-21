using AsyncMemoryCache.EvictionBehaviors;
using System;
using static AsyncMemoryCache.EvictionBehaviors.EvictionBehavior;

namespace AsyncMemoryCache;

public sealed class AsyncMemoryCacheConfiguration<T> where T : IAsyncDisposable
{
	public Action<string, T>? CacheItemExpired { get; init; }
	public IEvictionBehavior EvictionBehavior { get; init; } = Default;
}
