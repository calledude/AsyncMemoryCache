using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace AsyncMemoryCache.EvictionBehaviors;

[ExcludeFromCodeCoverage(Justification = "Nothing to test")]
internal sealed class NoOpEvictionBehavior : IEvictionBehavior
{
	public void Start<TKey, TValue>(IAsyncMemoryCacheConfiguration<TKey, TValue> configuration, ILogger<AsyncMemoryCache<TKey, TValue>>? logger)
		where TKey : notnull
		where TValue : IAsyncDisposable
	{
	}

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}