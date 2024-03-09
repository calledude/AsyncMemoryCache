using AsyncMemoryCache.EvictionBehaviors;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AsyncMemoryCache;

/// <summary>
/// A class representing the lifetime of the underlying cached object.<br/>
/// As long as this object is referenced/not disposed the cached object will not be evicted or disposed if <see cref="DefaultEvictionBehavior"/> is used.<br/>
/// </summary>
/// <typeparam name="TKey">The type of the key for the cache item.</typeparam>
/// <typeparam name="TValue">The type of the value which the referenced <see cref="CacheEntity{TKey, TValue}"/> wraps.</typeparam>
public sealed class CacheEntityReference<TKey, TValue> : IDisposable
	where TKey : notnull
	where TValue : IAsyncDisposable
{
	private bool _disposed;

	/// <summary>
	/// The <see cref="CacheEntity{TKey, TValue}"/> which this object wraps.
	/// </summary>
	public CacheEntity<TKey, TValue> CacheEntity { get; }

	/// <summary>
	/// Creates a new instance of <see cref="CacheEntityReference{TKey, TValue}"/> with the supplied arguments.
	/// </summary>
	/// <param name="cacheEntity">The <see cref="CacheEntity{TKey, TValue}"/> to wrap.</param>
	public CacheEntityReference(CacheEntity<TKey, TValue> cacheEntity)
	{
#if NET8_0_OR_GREATER
		ArgumentNullException.ThrowIfNull(cacheEntity);
#else
		if (cacheEntity is null)
			throw new ArgumentNullException(nameof(cacheEntity));
#endif

		CacheEntity = cacheEntity;
		_ = Interlocked.Increment(ref cacheEntity.References);
	}

	/// <summary>
	/// Asynchronous infrastructure support. This method permits instances of <see cref="CacheEntityReference{TKey, TValue}"/> to be await'ed.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public TaskAwaiter<TValue> GetAwaiter()
	{
		return CacheEntity.GetAwaiter();
	}

	/// <summary>
	/// Asynchronous infrastructure support. This method permits instances of <see cref="CacheEntityReference{TKey, TValue}"/> to be await'ed.
	/// </summary>
#if NET8_0_OR_GREATER
	[ExcludeFromCodeCoverage(Justification = "There's no real functionality to be tested here, it's just enabling await")]
#endif
	public ConfiguredTaskAwaitable<TValue> ConfigureAwait(bool continueOnCapturedContext)
	{
		return CacheEntity.ConfigureAwait(continueOnCapturedContext);
	}

	/// <summary>
	/// The finalizer for <see cref="CacheEntityReference{TKey, TValue}"/>.
	/// This serves as a fail-safe if <see cref="Dispose"/> is never called.
	/// </summary>
#if NET8_0_OR_GREATER
	[ExcludeFromCodeCoverage(Justification = "Finalizers are unreliable in tests")]
#endif
	~CacheEntityReference()
	{
		Dispose();
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;

		GC.SuppressFinalize(this);
		_ = Interlocked.Decrement(ref CacheEntity.References);
	}
}
