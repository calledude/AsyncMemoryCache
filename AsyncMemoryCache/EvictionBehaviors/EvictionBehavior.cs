using Microsoft.Extensions.Logging;
using System;

namespace AsyncMemoryCache.EvictionBehaviors;

/// <summary>
/// A class containing default values for implementations of <see cref="IEvictionBehavior"/>
/// </summary>
public static class EvictionBehavior
{
	/// <inheritdoc cref="DefaultEvictionBehavior"/>
#if NET8_0_OR_GREATER
	public static readonly IEvictionBehavior Default = new DefaultEvictionBehavior(TimeProvider.System);
#else
	public static readonly IEvictionBehavior Default = new DefaultEvictionBehavior();
#endif
	/// <inheritdoc cref="NoOpEvictionBehavior"/>
	public static readonly IEvictionBehavior Disabled = new NoOpEvictionBehavior();
}

/// <summary>
/// An interface that can be used to implement custom eviction behaviors.
/// See <see cref="EvictionBehavior"/> for default implementations.
/// </summary>
public interface IEvictionBehavior : IAsyncDisposable
{
	/// <summary>
	/// The method which starts the <see cref="IEvictionBehavior"/>.
	/// Called automatically by <see cref="AsyncMemoryCache{TKey, TValue}"/>.
	/// </summary>
	/// <typeparam name="TKey">The type of the key of the <see cref="CacheEntity{TKey, TValue}"/></typeparam>
	/// <typeparam name="TValue">The type of the value of the <see cref="CacheEntity{TKey, TValue}"/></typeparam>
	/// <param name="configuration">The configuration object used in <see cref="AsyncMemoryCache{TKey, TValue}"/> which contains the backing store that holds all of the cached objects</param>
	/// <param name="logger">The logger object</param>
	void Start<TKey, TValue>(IAsyncMemoryCacheConfiguration<TKey, TValue> configuration, ILogger<AsyncMemoryCache<TKey, TValue>> logger)
		where TKey : notnull
		where TValue : IAsyncDisposable;
}