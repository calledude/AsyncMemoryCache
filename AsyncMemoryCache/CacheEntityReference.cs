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
		CacheEntity = cacheEntity;
		_ = Interlocked.Increment(ref cacheEntity.References);
	}

	[ExcludeFromCodeCoverage(Justification = "Finalizers are unreliable in tests")]
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
