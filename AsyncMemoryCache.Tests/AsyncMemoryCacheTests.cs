using AsyncMemoryCache.EvictionBehaviors;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AsyncMemoryCache.Tests;

public class AsyncMemoryCacheTests
{
	[Fact]
	public async Task FactoryIsInvoked_DoesNotBlock()
	{
		var configuration = CreateConfiguration();
		var target = new AsyncMemoryCache<string, IAsyncDisposable>(configuration);

		var semaphore = new SemaphoreSlim(0, 1);

		var factory = () =>
		{
			semaphore.Wait();
			return Task.FromResult(Substitute.For<IAsyncDisposable>());
		};

		CacheEntity<string, IAsyncDisposable>? entity = null;
		var ex = await Record.ExceptionAsync(() => Task.Run(() => entity = target.Add("test", factory)).WaitAsync(TimeSpan.FromMilliseconds(500)));

		Assert.Null(ex);
		Assert.NotNull(entity);
		Assert.True(entity.ObjectFactory.IsStarted);
	}

	[Fact]
	public async Task Add_ObjectIsReturnedInCacheEntity()
	{
		var configuration = CreateConfiguration();
		var target = new AsyncMemoryCache<string, IAsyncDisposable>(configuration);

		var objectToCache = Substitute.For<IAsyncDisposable>();
		var factory = () => Task.FromResult(objectToCache);

		var cacheEntity = target.Add("test", factory);

		var cachedObject = await cacheEntity.ObjectFactory;
		Assert.Same(objectToCache, cachedObject);
	}

	[Fact]
	public async Task Add_ObjectIsReturnedFromIndexer()
	{
		var configuration = CreateConfiguration();
		var target = new AsyncMemoryCache<string, IAsyncDisposable>(configuration);

		var objectToCache = Substitute.For<IAsyncDisposable>();
		var factory = () => Task.FromResult(objectToCache);

		const string cacheKey = "test";
		_ = target.Add(cacheKey, factory);

		var cachedObject = await target[cacheKey];
		Assert.Same(objectToCache, cachedObject);
	}

	[Fact]
	public void Add_CalledTwice_ReturnsPreviousCacheEntity()
	{
		var configuration = CreateConfiguration();
		var target = new AsyncMemoryCache<string, IAsyncDisposable>(configuration);

		var factory = () => Task.FromResult(Substitute.For<IAsyncDisposable>());

		const string cacheKey = "test";
		var firstCacheEntity = target.Add(cacheKey, factory);

		var secondCacheEntity = target.Add(cacheKey, factory);

		Assert.Same(firstCacheEntity, secondCacheEntity);
	}

	[Fact]
	public void EvictionBehaviorIsStarted()
	{
		var evictionBehavior = Substitute.For<IEvictionBehavior>();
		var config = new AsyncMemoryCacheConfiguration<string, IAsyncDisposable>
		{
			EvictionBehavior = evictionBehavior
		};

		_ = new AsyncMemoryCache<string, IAsyncDisposable>(config);

		evictionBehavior
			.Received(1)
			.Start(
				config,
				Arg.Any<ILogger<AsyncMemoryCache<string, IAsyncDisposable>>>());
	}

	[Fact]
	public void ContainsKey()
	{
		var configuration = CreateConfiguration();
		var target = new AsyncMemoryCache<string, IAsyncDisposable>(configuration);

		var factory = () => Task.FromResult(Substitute.For<IAsyncDisposable>());

		const string cacheKey = "test1";
		_ = target.Add(cacheKey, factory);

		Assert.True(target.ContainsKey(cacheKey));
		Assert.False(target.ContainsKey("doesNotExist"));
	}

	[Fact]
	public async Task DisposeAsync()
	{
		var evictionBehavior = Substitute.For<IEvictionBehavior>();
		var target = new AsyncMemoryCache<string, IAsyncDisposable>(new AsyncMemoryCacheConfiguration<string, IAsyncDisposable>
		{
			EvictionBehavior = evictionBehavior
		});

		await target.DisposeAsync();

		await evictionBehavior.Received().DisposeAsync();
	}

	private static AsyncMemoryCacheConfiguration<string, IAsyncDisposable> CreateConfiguration()
	{
		return new()
		{
			EvictionBehavior = EvictionBehavior.Disabled
		};
	}
}