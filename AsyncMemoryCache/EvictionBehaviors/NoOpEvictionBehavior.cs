using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace AsyncMemoryCache.EvictionBehaviors;

/// <summary>
/// A no-op implementation of <see cref="IEvictionBehavior"/>.
/// This class has no behavior. Use this to disable eviction functionality.
/// </summary>
#if NET8_0_OR_GREATER
[ExcludeFromCodeCoverage(Justification = "Nothing to test")]
#endif
internal sealed class NoOpEvictionBehavior : IEvictionBehavior
{
	public void Start<TKey, TValue>(IAsyncMemoryCacheConfiguration<TKey, TValue> configuration, ILogger<AsyncMemoryCache<TKey, TValue>>? logger)
		where TKey : notnull
		where TValue : IAsyncDisposable
	{
	}

#if NET8_0_OR_GREATER
	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
#else
	public ValueTask DisposeAsync() => default;
#endif
}