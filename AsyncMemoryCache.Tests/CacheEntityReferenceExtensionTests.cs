using AsyncMemoryCache.Extensions;
using Nito.AsyncEx;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AsyncMemoryCache.Tests;

public class CacheEntityReferenceExtensionTests
{
	[Fact]
	public void WithAbsoluteExpirationExtension()
	{
		var expectedAbsoluteExpiration = DateTimeOffset.UtcNow;
		var cacheEntity = new CacheEntity<string, IAsyncDisposable>("test", () => Task.FromResult((IAsyncDisposable)null!), AsyncLazyFlags.None);
		var cacheEntityReference = new CacheEntityReference<string, IAsyncDisposable>(cacheEntity);

		cacheEntityReference.WithAbsoluteExpiration(expectedAbsoluteExpiration);

		Assert.Equal(expectedAbsoluteExpiration, cacheEntity.AbsoluteExpiration);
	}

	[Fact]
	public void WithSlidingExpirationExtension()
	{
		var expectedSlidingExpirationWindow = TimeSpan.FromMinutes(1);
		var cacheEntity = new CacheEntity<string, IAsyncDisposable>("test", () => Task.FromResult((IAsyncDisposable)null!), AsyncLazyFlags.None);
		var cacheEntityReference = new CacheEntityReference<string, IAsyncDisposable>(cacheEntity);

		cacheEntityReference.WithSlidingExpiration(expectedSlidingExpirationWindow);

		Assert.Equal(expectedSlidingExpirationWindow, cacheEntity.SlidingExpiration);
	}
}
