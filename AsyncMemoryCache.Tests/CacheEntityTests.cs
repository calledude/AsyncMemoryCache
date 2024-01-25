using Nito.AsyncEx;
using NSubstitute;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AsyncMemoryCache.Tests;

public class CacheEntityTests
{
	[Fact]
	public void WithAbsoluteExpiration()
	{
		var absoluteExpiration = DateTimeOffset.UtcNow;

		var cacheEntity = new CacheEntity<string, IAsyncDisposable>("key", () => Task.FromResult(Substitute.For<IAsyncDisposable>()), AsyncLazyFlags.None);
		var cacheEntityReturned = cacheEntity.WithAbsoluteExpiration(absoluteExpiration);

		Assert.Same(cacheEntity, cacheEntityReturned);
		Assert.Equal(absoluteExpiration, cacheEntityReturned.AbsoluteExpiration);
	}

	[Fact]
	public void WithSlidingExpiration()
	{
		var slidingExpiration = TimeSpan.FromMinutes(30);

		var cacheEntity = new CacheEntity<string, IAsyncDisposable>("key", () => Task.FromResult(Substitute.For<IAsyncDisposable>()), AsyncLazyFlags.None);
		var cacheEntityReturned = cacheEntity.WithSlidingExpiration(slidingExpiration);

		Assert.Same(cacheEntity, cacheEntityReturned);
		Assert.Equal(slidingExpiration, cacheEntityReturned.SlidingExpiration);
	}
}
