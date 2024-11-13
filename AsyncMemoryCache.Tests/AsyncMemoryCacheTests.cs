using AsyncMemoryCache.EvictionBehaviors;
using AsyncMemoryCache.ExpirationStrategy;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using NSubstitute;
using System;
using System.Collections.Generic;
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
		var target = AsyncMemoryCache<string, IAsyncDisposable>.Create(configuration);

		var semaphore = new SemaphoreSlim(0, 1);

		var factory = () =>
		{
			semaphore.Wait();
			return Task.FromResult(Substitute.For<IAsyncDisposable>());
		};

		CacheEntityReference<string, IAsyncDisposable>? cacheEntityReference = null;
		var ex = await Record.ExceptionAsync(() => Task.Run(() => cacheEntityReference = target.GetOrCreate("test", factory)).WaitAsync(TimeSpan.FromMilliseconds(500)));

		Assert.Null(ex);
		Assert.NotNull(cacheEntityReference);
		Assert.True(cacheEntityReference.CacheEntity.ObjectFactory.IsStarted);
	}

	[Fact]
	public async Task GetOrCreateObjectIsReturnedInCacheEntity()
	{
		var configuration = CreateConfiguration();
		var target = AsyncMemoryCache<string, IAsyncDisposable>.Create(configuration);

		var objectToCache = Substitute.For<IAsyncDisposable>();
		var factory = () => Task.FromResult(objectToCache);

		var cacheEntityReference = target.GetOrCreate("test", factory);

		var cachedObject = await cacheEntityReference.CacheEntity;
		Assert.Same(objectToCache, cachedObject);
	}

	[Fact]
	public async Task GetOrCreateObjectIsReturnedFromIndexer()
	{
		var configuration = CreateConfiguration();
		var target = AsyncMemoryCache<string, IAsyncDisposable>.Create(configuration);

		var objectToCache = Substitute.For<IAsyncDisposable>();
		var factory = () => Task.FromResult(objectToCache);

		const string cacheKey = "test";
		_ = target.GetOrCreate(cacheKey, factory);

		var cachedObject = await target[cacheKey];
		Assert.Same(objectToCache, cachedObject);
	}

	[Fact]
	public void GetOrCreateCalledTwice_ReturnsPreviousCacheEntity()
	{
		var configuration = CreateConfiguration();
		var target = AsyncMemoryCache<string, IAsyncDisposable>.Create(configuration);

		var factory = () => Task.FromResult(Substitute.For<IAsyncDisposable>());

		const string cacheKey = "test";
		var firstCacheEntity = target.GetOrCreate(cacheKey, factory).CacheEntity;
		var secondCacheEntity = target.GetOrCreate(cacheKey, factory).CacheEntity;

		Assert.Same(firstCacheEntity, secondCacheEntity);
	}

	[Fact]
	public void EvictionBehaviorIsStarted()
	{
		var evictionBehavior = Substitute.For<IEvictionBehavior>();
		var config = new AsyncMemoryCacheConfiguration<string, IAsyncDisposable>
		{
			EvictionBehavior = evictionBehavior,
		};

		_ = AsyncMemoryCache<string, IAsyncDisposable>.Create(config);

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
		var target = AsyncMemoryCache<string, IAsyncDisposable>.Create(configuration);

		var factory = () => Task.FromResult(Substitute.For<IAsyncDisposable>());

		const string cacheKey = "test1";
		_ = target.GetOrCreate(cacheKey, factory);

		Assert.True(target.ContainsKey(cacheKey));
		Assert.False(target.ContainsKey("doesNotExist"));
	}

	[Fact]
	public async Task DisposeAsync()
	{
		var cacheObject1 = Substitute.For<IAsyncDisposable>();
		var cacheObject2 = Substitute.For<IAsyncDisposable>();

		var evictionBehavior = Substitute.For<IEvictionBehavior>();
		var config = new AsyncMemoryCacheConfiguration<string, IAsyncDisposable>
		{
			EvictionBehavior = evictionBehavior,
			CacheBackingStore = new Dictionary<string, CacheEntity<string, IAsyncDisposable>>
			{
				{ "test1", new CacheEntity<string, IAsyncDisposable>("test1", () => Task.FromResult(cacheObject1), AsyncLazyFlags.None) },
				{ "test2", new CacheEntity<string, IAsyncDisposable>("test2", () => Task.FromResult(cacheObject2), AsyncLazyFlags.None ) },
			},
		};

		var target = AsyncMemoryCache<string, IAsyncDisposable>.Create(config);

		await target.DisposeAsync();

		await evictionBehavior.Received(1).DisposeAsync();

		await cacheObject1.Received(1).DisposeAsync();
		await cacheObject2.Received(1).DisposeAsync();
	}

	[Fact]
	public void NewCacheEntityReferenceReturnedOnEveryCall()
	{
		var cacheObject = Substitute.For<IAsyncDisposable>();

		var evictionBehavior = Substitute.For<IEvictionBehavior>();
		var config = new AsyncMemoryCacheConfiguration<string, IAsyncDisposable>
		{
			EvictionBehavior = evictionBehavior,
			CacheBackingStore = new Dictionary<string, CacheEntity<string, IAsyncDisposable>>(),
		};

		var target = AsyncMemoryCache<string, IAsyncDisposable>.Create(config);

		const string key = "test";
		var cacheEntityReference1 = target.GetOrCreate(key, () => Task.FromResult(cacheObject));
		var cacheEntityReference2 = target.GetOrCreate(key, () => Task.FromResult(cacheObject));
		var cacheEntityReference3 = target[key];

		Assert.NotSame(cacheEntityReference1, cacheEntityReference2);
		Assert.NotSame(cacheEntityReference2, cacheEntityReference3);
		Assert.NotSame(cacheEntityReference1, cacheEntityReference3);

		Assert.Same(cacheEntityReference1.CacheEntity, cacheEntityReference2.CacheEntity);
		Assert.Same(cacheEntityReference2.CacheEntity, cacheEntityReference3.CacheEntity);
		Assert.Same(cacheEntityReference1.CacheEntity, cacheEntityReference3.CacheEntity);

		Assert.Equal(3, cacheEntityReference1.CacheEntity.References);
	}

	[Fact]
	public void NewCacheEntityIsCreatedIfUsesIsBelowZero()
	{
		var cacheObject = Substitute.For<IAsyncDisposable>();

		var evictionBehavior = Substitute.For<IEvictionBehavior>();
		var config = new AsyncMemoryCacheConfiguration<string, IAsyncDisposable>
		{
			EvictionBehavior = evictionBehavior,
			CacheBackingStore = new Dictionary<string, CacheEntity<string, IAsyncDisposable>>(),
		};

		var target = AsyncMemoryCache<string, IAsyncDisposable>.Create(config);

		const string key = "test";
		var cacheEntityReference1 = target.GetOrCreate(key, () => Task.FromResult(cacheObject));

		//Simulate evictionbehavior setting this to -1
		cacheEntityReference1.CacheEntity.References = -1;

		var cacheEntityReference2 = target.GetOrCreate(key, () => Task.FromResult(cacheObject));

		Assert.NotSame(cacheEntityReference1, cacheEntityReference2);
		Assert.NotSame(cacheEntityReference1.CacheEntity, cacheEntityReference2.CacheEntity);

		Assert.Equal(-1, cacheEntityReference1.CacheEntity.References);
		Assert.Equal(1, cacheEntityReference2.CacheEntity.References);
	}

	[Fact]
	public void TryGetValue_ReturnsFalseIfItemDoesNotExist()
	{
		var evictionBehavior = Substitute.For<IEvictionBehavior>();
		var config = new AsyncMemoryCacheConfiguration<string, IAsyncDisposable>
		{
			EvictionBehavior = evictionBehavior,
			CacheBackingStore = new Dictionary<string, CacheEntity<string, IAsyncDisposable>>(),
		};

		var target = AsyncMemoryCache<string, IAsyncDisposable>.Create(config);

		var exists = target.TryGetValue("doesNotExist", out var cacheEntityReference);

		Assert.False(exists);
		Assert.Null(cacheEntityReference);
	}

	[Fact]
	public void TryGetValue_ReturnsFalseWhenBeingDisposed()
	{
		const string key = "test";

		var evictionBehavior = Substitute.For<IEvictionBehavior>();
		var config = new AsyncMemoryCacheConfiguration<string, IAsyncDisposable>
		{
			EvictionBehavior = evictionBehavior,
			CacheBackingStore = new Dictionary<string, CacheEntity<string, IAsyncDisposable>>
			{
				{ key, new(key, () => Task.FromResult(Substitute.For<IAsyncDisposable>()), AsyncLazyFlags.None) },
			},
		};

		config.CacheBackingStore[key].References = -1;

		var target = AsyncMemoryCache<string, IAsyncDisposable>.Create(config);

		var exists = target.TryGetValue(key, out var cacheEntityReference);

		Assert.False(exists);
		Assert.Null(cacheEntityReference);
		Assert.Equal(-1, config.CacheBackingStore[key].References);
	}

	[Fact]
	public void TryGetValue_ReturnsTrueIfItemExistsAndIncrementsReferenceCount()
	{
		const string key = "test";

		var evictionBehavior = Substitute.For<IEvictionBehavior>();
		var config = new AsyncMemoryCacheConfiguration<string, IAsyncDisposable>
		{
			EvictionBehavior = evictionBehavior,
			CacheBackingStore = new Dictionary<string, CacheEntity<string, IAsyncDisposable>>
			{
				{ key, new(key, () => Task.FromResult(Substitute.For<IAsyncDisposable>()), AsyncLazyFlags.None) },
			},
		};

		var target = AsyncMemoryCache<string, IAsyncDisposable>.Create(config);

		var exists = target.TryGetValue(key, out var cacheEntityReference);

		Assert.True(exists);
		Assert.NotNull(cacheEntityReference);
		Assert.Equal(1, cacheEntityReference.CacheEntity.References);
	}

	[Fact]
	public void TryGetValue_ExistingItemWithExpirationStrategy_CallsCacheEntityAccessed()
	{
		const string key = "test";

		var expirationStrategy = Substitute.For<IExpirationStrategy>();

		var config = new AsyncMemoryCacheConfiguration<string, IAsyncDisposable>
		{
			CacheBackingStore = new Dictionary<string, CacheEntity<string, IAsyncDisposable>>
			{
				{
					key,
					new CacheEntity<string, IAsyncDisposable>(key, () => Task.FromResult(Substitute.For<IAsyncDisposable>()), AsyncLazyFlags.None)
						.WithExpirationStrategy(expirationStrategy)
				},
			},
		};

		var target = AsyncMemoryCache<string, IAsyncDisposable>.Create(config);

		_ = target.TryGetValue(key, out var cacheEntityReference);

		expirationStrategy.Received(1).CacheEntityAccessed();
	}

	[Fact]
	public void Indexer_ExistingItemWithExpirationStrategy_CallsCacheEntityAccessed()
	{
		const string key = "test";

		var expirationStrategy = Substitute.For<IExpirationStrategy>();

		var config = new AsyncMemoryCacheConfiguration<string, IAsyncDisposable>
		{
			CacheBackingStore = new Dictionary<string, CacheEntity<string, IAsyncDisposable>>
			{
				{
					key,
					new CacheEntity<string, IAsyncDisposable>(key, () => Task.FromResult(Substitute.For<IAsyncDisposable>()), AsyncLazyFlags.None)
						.WithExpirationStrategy(expirationStrategy)
				},
			},
		};

		var target = AsyncMemoryCache<string, IAsyncDisposable>.Create(config);

		_ = target[key];

		expirationStrategy.Received(1).CacheEntityAccessed();
	}

	private static AsyncMemoryCacheConfiguration<string, IAsyncDisposable> CreateConfiguration()
	{
		return new()
		{
			EvictionBehavior = EvictionBehavior.Disabled,
		};
	}
}