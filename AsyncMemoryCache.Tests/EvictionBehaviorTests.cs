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
	[Theory]
	[InlineData(null, 30)]
	[InlineData(5, 5)]
	[InlineData(8, 8)]
	[InlineData(900, 900)]
	public void DefaultEvictionBehavior_TicksAccordingToConfig(int? seconds, int expectedTickInterval)
	{
		var config = new AsyncMemoryCacheConfiguration<IAsyncDisposable>();
		var cache = Substitute.For<IDictionary<string, CacheEntity<IAsyncDisposable>>>();

		var resetEvent = new ManualResetEvent(false);
		_ = cache.Configure()
			.Values
			.Returns([])
			.AndDoes(_ => resetEvent.Set());

		var timeProvider = new FakeTimeProvider();

		TimeSpan? interval = seconds.HasValue
			? TimeSpan.FromSeconds(seconds.Value)
			: null;

		var target = new DefaultEvictionBehavior(timeProvider, interval);
		target.Start(cache, config);

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

		var cache = new Dictionary<string, CacheEntity<IAsyncDisposable>>
		{
			{
				notExpiredKey, new CacheEntity<IAsyncDisposable>(notExpiredKey, () => Task.FromResult(notExpiredCacheObject), AsyncLazyFlags.None)
				{
					Lifetime = TimeSpan.FromDays(2)
				}
			},
			{
				expiredKey, new CacheEntity<IAsyncDisposable>(expiredKey, () => Task.FromResult(expiredCacheObject), AsyncLazyFlags.None)
				{
					Lifetime = TimeSpan.FromTicks(1)
				}
			}
		};

		var timeProvider = new FakeTimeProvider(DateTime.UtcNow);

		var target = new DefaultEvictionBehavior(timeProvider);

		var evt = new ManualResetEvent(false);

		var expiredCacheItems = new Dictionary<string, IAsyncDisposable>();
		var config = new AsyncMemoryCacheConfiguration<IAsyncDisposable>
		{
			CacheItemExpired = async (s, item) =>
			{
				expiredCacheItems[s] = item;

				// Bit of a hack
				// We need to wait for it to complete one tick, and DisposeAsync() waits for the worker task to complete
				// And as such guarantees at least one tick completion
				await target.DisposeAsync();
				_ = evt.Set();
			}
		};

		target.Start(cache, config);

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
}
