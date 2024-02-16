using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace AsyncMemoryCache;

public sealed class CacheEntityReference<TKey, TValue> : IDisposable
	where TKey : notnull
	where TValue : IAsyncDisposable
{
	private bool _disposed;

	public CacheEntity<TKey, TValue> CacheEntity { get; }

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

#if NET8_0_OR_GREATER
	[ExcludeFromCodeCoverage(Justification = "Finalizers are unreliable in tests")]
#endif
	~CacheEntityReference()
	{
		Dispose();
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;

		GC.SuppressFinalize(this);
		_ = Interlocked.Decrement(ref CacheEntity.References);
	}
}
