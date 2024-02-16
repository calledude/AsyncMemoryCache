using Microsoft.Extensions.Logging;
using System;

namespace AsyncMemoryCache.EvictionBehaviors;

public static class EvictionBehavior
{
#if NET8_0_OR_GREATER
	public static readonly IEvictionBehavior Default = new DefaultEvictionBehavior(TimeProvider.System);
#else
	public static readonly IEvictionBehavior Default = new DefaultEvictionBehavior();
#endif
	public static readonly IEvictionBehavior Disabled = new NoOpEvictionBehavior();
}

public interface IEvictionBehavior : IAsyncDisposable
{
	void Start<TKey, TValue>(IAsyncMemoryCacheConfiguration<TKey, TValue> configuration, ILogger<AsyncMemoryCache<TKey, TValue>> logger)
		where TKey : notnull
		where TValue : IAsyncDisposable;
}