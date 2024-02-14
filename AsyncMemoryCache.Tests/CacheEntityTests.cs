using AsyncMemoryCache.ExpirationStrategy;
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

		var absoluteExpirationStrategy = cacheEntityReturned.ExpirationStrategy as AbsoluteExpirationStrategy;
		Assert.NotNull(absoluteExpirationStrategy);
		Assert.Equal(absoluteExpiration, absoluteExpirationStrategy.AbsoluteExpiration);
	}

	[Fact]
	public void WithSlidingExpiration()
	{
		var slidingExpiration = TimeSpan.FromMinutes(30);

		var cacheEntity = new CacheEntity<string, IAsyncDisposable>("key", () => Task.FromResult(Substitute.For<IAsyncDisposable>()), AsyncLazyFlags.None);
		var cacheEntityReturned = cacheEntity.WithSlidingExpiration(slidingExpiration);

		Assert.Same(cacheEntity, cacheEntityReturned);

		var slidingExpirationStrategy = cacheEntityReturned.ExpirationStrategy as SlidingExpirationStrategy;
		Assert.NotNull(slidingExpirationStrategy);
		Assert.Equal(slidingExpiration, slidingExpirationStrategy.SlidingExpirationWindow);
	}

	[Fact]
	public void WithoutAnyExpirationStrategy()
	{
		var cacheEntity = new CacheEntity<string, IAsyncDisposable>("key", () => Task.FromResult(Substitute.For<IAsyncDisposable>()), AsyncLazyFlags.None);
		Assert.Null(cacheEntity.ExpirationStrategy);
	}
}
