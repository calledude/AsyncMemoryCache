using AsyncMemoryCache.ExpirationStrategy;
using AsyncMemoryCache.Extensions;
using Nito.AsyncEx;
using NSubstitute;
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

		var absoluteExpirationStrategy = cacheEntity.ExpirationStrategy as AbsoluteExpirationStrategy;
		Assert.NotNull(absoluteExpirationStrategy);
		Assert.Equal(expectedAbsoluteExpiration, absoluteExpirationStrategy.AbsoluteExpiration);
	}

	[Fact]
	public void WithSlidingExpirationExtension()
	{
		var expectedSlidingExpirationWindow = TimeSpan.FromMinutes(1);
		var cacheEntity = new CacheEntity<string, IAsyncDisposable>("test", () => Task.FromResult((IAsyncDisposable)null!), AsyncLazyFlags.None);
		var cacheEntityReference = new CacheEntityReference<string, IAsyncDisposable>(cacheEntity);

		cacheEntityReference.WithSlidingExpiration(expectedSlidingExpirationWindow);

		var slidingExpirationStrategy = cacheEntity.ExpirationStrategy as SlidingExpirationStrategy;
		Assert.NotNull(slidingExpirationStrategy);
		Assert.Equal(expectedSlidingExpirationWindow, slidingExpirationStrategy.SlidingExpirationWindow);
	}

	[Fact]
	public void WithExpirationStrategyExtension()
	{
		var expirationStrategy = Substitute.For<IExpirationStrategy>();

		var cacheEntity = new CacheEntity<string, IAsyncDisposable>("test", () => Task.FromResult((IAsyncDisposable)null!), AsyncLazyFlags.None);
		var cacheEntityReference = new CacheEntityReference<string, IAsyncDisposable>(cacheEntity);

		cacheEntityReference.WithExpirationStrategy(expirationStrategy);

		Assert.Equal(expirationStrategy, cacheEntity.ExpirationStrategy);
	}

	[Fact]
	public void WithExpirationCallbackExtension()
	{
		var expirationCallback = (string _, IAsyncDisposable _) => { };

		var cacheEntity = new CacheEntity<string, IAsyncDisposable>("test", () => Task.FromResult((IAsyncDisposable)null!), AsyncLazyFlags.None);
		var cacheEntityReference = new CacheEntityReference<string, IAsyncDisposable>(cacheEntity);

		cacheEntityReference.WithExpirationCallback(expirationCallback);

		Assert.Equal(expirationCallback, cacheEntity.ExpirationCallback);
	}
}
