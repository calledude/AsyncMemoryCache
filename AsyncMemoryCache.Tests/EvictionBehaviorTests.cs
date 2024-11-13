using AsyncMemoryCache.EvictionBehaviors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Nito.AsyncEx;
using NSubstitute;
using NSubstitute.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AsyncMemoryCache.Tests;

public class EvictionBehaviorTests
{
	private static readonly ILogger<AsyncMemoryCache<string, IAsyncDisposable>> _logger = NullLoggerFactory.Instance.CreateLogger<AsyncMemoryCache<string, IAsyncDisposable>>();

	[Theory]
	[InlineData(null, 30)]
	[InlineData(5, 5)]
	[InlineData(8, 8)]
	[InlineData(900, 900)]
	public void DefaultEvictionBehavior_TicksAccordingToConfig(int? seconds, int expectedTickInterval)
	{
		var resetEvent = new ManualResetEvent(false);

		var cache = Substitute.For<IDictionary<string, CacheEntity<string, IAsyncDisposable>>>();
		_ = cache.Configure()
			.Values
			.Returns([])
			.AndDoes(_ => resetEvent.Set());

		var config = new AsyncMemoryCacheConfiguration<string, IAsyncDisposable>
		{
			CacheBackingStore = cache,
		};

		var timeProvider = new FakeTimeProvider();

		TimeSpan? interval = seconds.HasValue
			? TimeSpan.FromSeconds(seconds.Value)
			: null;

		var target = new DefaultEvictionBehavior(timeProvider, interval);
		target.Start(config, _logger);

		timeProvider.Advance(TimeSpan.FromSeconds(expectedTickInterval - 0.1));

		Assert.Empty(cache.ReceivedCalls());

		timeProvider.Advance(TimeSpan.FromSeconds(0.1));

		var signalled = resetEvent.WaitOne();
		Assert.True(signalled);
		Assert.NotEmpty(cache.ReceivedCalls());
	}

	[Fact]
	public async Task DefaultEvictionBehavior_RemovesAbsoluteExpiredItem_LeavesNonExpiredItems()
	{
		var notExpiredCacheObject = Substitute.For<IAsyncDisposable>();
		var expiredCacheObject = Substitute.For<IAsyncDisposable>();

		const string expiredKey = "expired";
		const string notExpiredKey = "notExpired";

		var expiredItem = new CacheEntity<string, IAsyncDisposable>(expiredKey, () => Task.FromResult(expiredCacheObject), AsyncLazyFlags.None)
					.WithAbsoluteExpiration(DateTimeOffset.UtcNow.AddMinutes(-1));

		var notExpiredItem = new CacheEntity<string, IAsyncDisposable>(notExpiredKey, () => Task.FromResult(notExpiredCacheObject), AsyncLazyFlags.None)
					.WithAbsoluteExpiration(DateTimeOffset.UtcNow.AddDays(2));

		await Internal_DefaultEvictionBehavior_RemovesExpiredItem_LeavesNonExpiredItems(expiredItem, notExpiredItem, expiredCacheObject, notExpiredCacheObject);
	}

	[Fact]
	public async Task DefaultEvictionBehavior_RemovesSlidingExpiredItem_LeavesNonExpiredItems()
	{
		var notExpiredCacheObject = Substitute.For<IAsyncDisposable>();
		var expiredCacheObject = Substitute.For<IAsyncDisposable>();

		const string expiredKey = "expired";
		const string notExpiredKey = "notExpired";

		var expiredItem = new CacheEntity<string, IAsyncDisposable>(expiredKey, () => Task.FromResult(expiredCacheObject), AsyncLazyFlags.None)
					.WithSlidingExpiration(TimeSpan.FromTicks(1));

		var notExpiredItem = new CacheEntity<string, IAsyncDisposable>(notExpiredKey, () => Task.FromResult(notExpiredCacheObject), AsyncLazyFlags.None)
					.WithSlidingExpiration(TimeSpan.FromDays(2));

		await Internal_DefaultEvictionBehavior_RemovesExpiredItem_LeavesNonExpiredItems(expiredItem, notExpiredItem, expiredCacheObject, notExpiredCacheObject);
	}

	private static async Task Internal_DefaultEvictionBehavior_RemovesExpiredItem_LeavesNonExpiredItems(
		CacheEntity<string, IAsyncDisposable> expiredItem,
		CacheEntity<string, IAsyncDisposable> notExpiredItem,
		IAsyncDisposable expiredCacheObject,
		IAsyncDisposable notExpiredCacheObject)
	{
		var timeProvider = new FakeTimeProvider(DateTime.UtcNow);
		var target = new DefaultEvictionBehavior(timeProvider);

		var resetEvent = new ManualResetEvent(false);
		var cache = Substitute.For<IDictionary<string, CacheEntity<string, IAsyncDisposable>>>();
		_ = cache.Configure()
			.Values
			.Returns([expiredItem, notExpiredItem])
			.AndDoes(_ => resetEvent.Set());

		cache.Remove(Arg.Any<string>()).Returns(true);

		var config = new AsyncMemoryCacheConfiguration<string, IAsyncDisposable>
		{
			CacheBackingStore = cache,
		};

		target.Start(config, _logger);

		timeProvider.Advance(TimeSpan.FromSeconds(30));

		// Will ensure that the internal loop has completed at least one tick
		_ = resetEvent.WaitOne();
		await target.DisposeAsync();

		cache.Received(1).Remove(expiredItem.Key);
		cache.DidNotReceive().Remove(notExpiredItem.Key);

		await expiredCacheObject.Received(1).DisposeAsync();
		await notExpiredCacheObject.DidNotReceive().DisposeAsync();
	}

	[Fact]
	public async Task UseSystemTimer()
	{
		var resetEvent = new ManualResetEvent(false);
		var cache = Substitute.For<IDictionary<string, CacheEntity<string, IAsyncDisposable>>>();
		_ = cache.Configure()
			.Values
			.Returns([])
			.AndDoes(_ => resetEvent.Set());

		var config = new AsyncMemoryCacheConfiguration<string, IAsyncDisposable>
		{
			CacheBackingStore = cache,
		};

		var target = new DefaultEvictionBehavior(null, TimeSpan.FromMilliseconds(1));
		target.Start(config, _logger);

		_ = resetEvent.WaitOne();
		await target.DisposeAsync();

		_ = cache.Received().Values;
	}

	[Fact]
	public async Task ExpiredCacheItem_AlwaysDisposedIfRefCountBelowZero()
	{
		var expiredCacheObject = Substitute.For<IAsyncDisposable>();

		const string expiredKey = "expired";

		var resetEvent = new ManualResetEvent(false);
		var cache = Substitute.For<IDictionary<string, CacheEntity<string, IAsyncDisposable>>>();
		_ = cache.Configure()
			.Values
			.Returns(
			[
				new CacheEntity<string, IAsyncDisposable>(expiredKey, () => Task.FromResult(expiredCacheObject), AsyncLazyFlags.None)
					.WithAbsoluteExpiration(DateTimeOffset.UtcNow)
			])
			.AndDoes(_ => resetEvent.Set());

		_ = cache.Remove(expiredKey).Returns(false);

		var timeProvider = new FakeTimeProvider(DateTime.UtcNow);
		var target = new DefaultEvictionBehavior(timeProvider);
		var config = new AsyncMemoryCacheConfiguration<string, IAsyncDisposable>
		{
			CacheItemExpired = null,
			CacheBackingStore = cache,
		};

		target.Start(config, _logger);

		timeProvider.Advance(TimeSpan.FromSeconds(30));

		_ = resetEvent.WaitOne();
		await target.DisposeAsync();

		await expiredCacheObject.Received(1).DisposeAsync();
	}

	[Fact]
	public async Task ExpiredCacheItem_NeverDisposedIfRefCountAboveZero()
	{
		var expiredCacheObject = Substitute.For<IAsyncDisposable>();
		const string expiredKey = "expired";
		var cacheBackingStore = new Dictionary<string, CacheEntity<string, IAsyncDisposable>>
		{
			{
				expiredKey, new CacheEntity<string, IAsyncDisposable>(expiredKey, () => Task.FromResult(expiredCacheObject), AsyncLazyFlags.None)
					.WithAbsoluteExpiration(DateTimeOffset.UtcNow)
			},
		};

		var resetEvent = new ManualResetEvent(false);
		var config = Substitute.For<IAsyncMemoryCacheConfiguration<string, IAsyncDisposable>>();
		_ = config.CacheBackingStore
			.Returns(cacheBackingStore)
			.AndDoes(_ => resetEvent.Set());

		cacheBackingStore[expiredKey].References++;

		var timeProvider = new FakeTimeProvider(DateTime.UtcNow);
		var target = new DefaultEvictionBehavior(timeProvider);
		target.Start(config, _logger);

		timeProvider.Advance(TimeSpan.FromSeconds(30));

		_ = resetEvent.WaitOne();
		await target.DisposeAsync();

		await expiredCacheObject.DidNotReceive().DisposeAsync();
	}

	[Fact]
	public async Task CacheItemWithoutExpirationStrategy_NeverDisposed()
	{
		var expiredCacheObject = Substitute.For<IAsyncDisposable>();
		const string expiredKey = "expired";
		var cacheBackingStore = new Dictionary<string, CacheEntity<string, IAsyncDisposable>>
		{
			{
				// Not setting any expiration strategy
				expiredKey, new CacheEntity<string, IAsyncDisposable>(expiredKey, () => Task.FromResult(expiredCacheObject), AsyncLazyFlags.None)
			},
		};

		var resetEvent = new ManualResetEvent(false);
		var config = Substitute.For<IAsyncMemoryCacheConfiguration<string, IAsyncDisposable>>();
		_ = config.CacheBackingStore
			.Returns(cacheBackingStore)
			.AndDoes(_ => resetEvent.Set());

		var timeProvider = new FakeTimeProvider(DateTime.UtcNow);
		var target = new DefaultEvictionBehavior(timeProvider);
		target.Start(config, _logger);

		timeProvider.Advance(TimeSpan.FromDays(30));

		_ = resetEvent.WaitOne();
		await target.DisposeAsync();

		await expiredCacheObject.DidNotReceive().DisposeAsync();
	}

	[Fact]
	public async Task DefaultEvictionBehavior_CallsCallbacks_ForExpiredItem()
	{
		const string expiredKey = "expired";

		var resetEvent = new ManualResetEvent(false);
		var cacheItemExpirationCallbackCalled = false;
		var cacheItemExpirationCallback = (string _, IAsyncDisposable _) =>
		{
			cacheItemExpirationCallbackCalled = true;
			resetEvent.Set();
		};

		var expiredCacheObject = Substitute.For<IAsyncDisposable>();
		var cache = Substitute.For<IDictionary<string, CacheEntity<string, IAsyncDisposable>>>();
		_ = cache.Configure()
			.Values
			.Returns(
			[
				new CacheEntity<string, IAsyncDisposable>(expiredKey, () => Task.FromResult(expiredCacheObject), AsyncLazyFlags.None)
					.WithAbsoluteExpiration(DateTimeOffset.UtcNow)
					.WithExpirationCallback(cacheItemExpirationCallback)
			])
			.AndDoes(_ => resetEvent.Set());

		cache.Remove(expiredKey).Returns(true);

		var timeProvider = new FakeTimeProvider(DateTime.UtcNow);
		var target = new DefaultEvictionBehavior(timeProvider);
		var globalCacheItemExpiredCallbackCalled = false;
		var config = new AsyncMemoryCacheConfiguration<string, IAsyncDisposable>
		{
			CacheItemExpired = (string _, IAsyncDisposable _) => globalCacheItemExpiredCallbackCalled = true,
			CacheBackingStore = cache,
		};

		target.Start(config, _logger);

		timeProvider.Advance(TimeSpan.FromSeconds(30));
		resetEvent.WaitOne();

		await target.DisposeAsync();

		Assert.True(cacheItemExpirationCallbackCalled);
		Assert.True(globalCacheItemExpiredCallbackCalled);
	}
}
