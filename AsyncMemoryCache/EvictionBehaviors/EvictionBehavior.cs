using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace AsyncMemoryCache.EvictionBehaviors;

public static class EvictionBehavior
{
	public static readonly IEvictionBehavior Default = new DefaultEvictionBehavior(TimeProvider.System);
	public static readonly IEvictionBehavior Disabled = new NoOpEvictionBehavior();
}

public interface IEvictionBehavior : IAsyncDisposable
{
	void Start<T>(IDictionary<string, CacheEntity<T>> cache, AsyncMemoryCacheConfiguration<T> configuration, ILogger<AsyncMemoryCache<T>> logger) where T : IAsyncDisposable;
}