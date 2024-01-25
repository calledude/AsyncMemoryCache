﻿using AsyncMemoryCache.EvictionBehaviors;
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
			CacheBackingStore = cache
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
	public async Task DefaultEvictionBehavior_RemovesExpiredItem_LeavesNonExpiredItems()
	{
		var notExpiredCacheObject = Substitute.For<IAsyncDisposable>();
		var expiredCacheObject = Substitute.For<IAsyncDisposable>();

		const string expiredKey = "expired";
		const string notExpiredKey = "notExpired";

		var cache = new Dictionary<string, CacheEntity<string, IAsyncDisposable>>
		{
			{
				notExpiredKey, new CacheEntity<string, IAsyncDisposable>(notExpiredKey, () => Task.FromResult(notExpiredCacheObject), AsyncLazyFlags.None)
				{
					Lifetime = TimeSpan.FromDays(2)
				}
			},
			{
				expiredKey, new CacheEntity<string, IAsyncDisposable>(expiredKey, () => Task.FromResult(expiredCacheObject), AsyncLazyFlags.None)
				{
					Lifetime = TimeSpan.FromTicks(1)
				}
			}
		};

		var timeProvider = new FakeTimeProvider(DateTime.UtcNow);

		var target = new DefaultEvictionBehavior(timeProvider);

		var evt = new ManualResetEvent(false);

		var expiredCacheItems = new Dictionary<string, IAsyncDisposable>();
		var config = new AsyncMemoryCacheConfiguration<string, IAsyncDisposable>
		{
			CacheItemExpired = async (s, item) =>
			{
				expiredCacheItems[s] = item;

				// Bit of a hack
				// We need to wait for it to complete one tick, and DisposeAsync() waits for the worker task to complete
				// And as such guarantees at least one tick completion
				await target.DisposeAsync();
				_ = evt.Set();
			},
			CacheBackingStore = cache
		};

		target.Start(config, _logger);

		timeProvider.Advance(TimeSpan.FromSeconds(30));

		_ = evt.WaitOne();

		Assert.True(cache.ContainsKey(notExpiredKey));
		Assert.False(cache.ContainsKey(expiredKey));

		Assert.True(expiredCacheItems.ContainsKey(expiredKey));
		Assert.False(expiredCacheItems.ContainsKey(notExpiredKey));

		Assert.Same(expiredCacheObject, expiredCacheItems[expiredKey]);

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
			CacheBackingStore = cache
		};

		var target = new DefaultEvictionBehavior(null, TimeSpan.FromMilliseconds(1));
		target.Start(config, _logger);

		_ = resetEvent.WaitOne();
		await target.DisposeAsync();

		_ = cache.Received().Values;
	}

	[Fact]
	public async Task ExpiredCacheItemIsAlwaysDisposed()
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
				{
					Lifetime = TimeSpan.FromTicks(1)
				}
			])
			.AndDoes(_ => resetEvent.Set());

		_ = cache.Remove(expiredKey).Returns(false);

		var timeProvider = new FakeTimeProvider(DateTime.UtcNow);
		var target = new DefaultEvictionBehavior(timeProvider);
		var config = new AsyncMemoryCacheConfiguration<string, IAsyncDisposable>
		{
			CacheItemExpired = null,
			CacheBackingStore = cache
		};

		target.Start(config, _logger);

		timeProvider.Advance(TimeSpan.FromSeconds(30));

		_ = resetEvent.WaitOne();
		await target.DisposeAsync();

		await expiredCacheObject.Received(1).DisposeAsync();
	}
}