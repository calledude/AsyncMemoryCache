using Nito.AsyncEx;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AsyncMemoryCache.Tests;

public class CacheEntityReferenceTests
{
	[Fact]
	public void IncrementsAndDecrementsProperly()
	{
		var cacheEntity = new CacheEntity<string, IAsyncDisposable>(string.Empty, () => Task.FromResult((IAsyncDisposable)null!), AsyncLazyFlags.None);
		Assert.Equal(0, cacheEntity.References);

		var cacheEntityReference = new CacheEntityReference<string, IAsyncDisposable>(cacheEntity);
		Assert.Equal(1, cacheEntity.References);

		cacheEntityReference.Dispose();
		Assert.Equal(0, cacheEntity.References);
	}

	[Fact]
	public void MultipleDisposes_DecrementsOnlyOnce()
	{
		var cacheEntity = new CacheEntity<string, IAsyncDisposable>(string.Empty, () => Task.FromResult((IAsyncDisposable)null!), AsyncLazyFlags.None);
		Assert.Equal(0, cacheEntity.References);

		var cacheEntityReference = new CacheEntityReference<string, IAsyncDisposable>(cacheEntity);
		Assert.Equal(1, cacheEntity.References);
#pragma warning disable
		cacheEntityReference.Dispose();
		cacheEntityReference.Dispose();
		cacheEntityReference.Dispose();
		cacheEntityReference.Dispose();
		cacheEntityReference.Dispose();
#pragma warning restore
		Assert.Equal(0, cacheEntity.References);
	}
}
