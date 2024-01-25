using Microsoft.Extensions.Logging;
using System;

namespace AsyncMemoryCache.EvictionBehaviors;

public static class EvictionBehavior
{
	public static readonly IEvictionBehavior Default = new DefaultEvictionBehavior(TimeProvider.System);
	public static readonly IEvictionBehavior Disabled = new NoOpEvictionBehavior();
}

public interface IEvictionBehavior : IAsyncDisposable
{
	void Start<TKey, TValue>(AsyncMemoryCacheConfiguration<TKey, TValue> configuration, ILogger<AsyncMemoryCache<TKey, TValue>> logger)
		where TKey : notnull
		where TValue : IAsyncDisposable;
}