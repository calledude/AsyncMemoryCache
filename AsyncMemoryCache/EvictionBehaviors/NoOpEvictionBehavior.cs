using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace AsyncMemoryCache.EvictionBehaviors;

[ExcludeFromCodeCoverage(Justification = "Nothing to test")]
internal sealed class NoOpEvictionBehavior : IEvictionBehavior
{
	public void Start<T>(IDictionary<string, CacheEntity<T>> cache, AsyncMemoryCacheConfiguration<T> configuration, ILogger<AsyncMemoryCache<T>>? logger) where T : IAsyncDisposable { }
	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}