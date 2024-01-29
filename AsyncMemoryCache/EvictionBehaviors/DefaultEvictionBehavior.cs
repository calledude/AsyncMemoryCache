using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncMemoryCache.EvictionBehaviors;

public sealed class DefaultEvictionBehavior : IEvictionBehavior
{
	private readonly PeriodicTimer _timer;
	private readonly CancellationTokenSource _cts;
	private Task? _workerTask;

	public DefaultEvictionBehavior(TimeProvider? timeProvider = default, TimeSpan? evictionCheckInterval = default)
	{
		var interval = evictionCheckInterval ?? TimeSpan.FromSeconds(30);
		_timer = new PeriodicTimer(interval, timeProvider ?? TimeProvider.System);
		_cts = new CancellationTokenSource();
	}

	public void Start<TKey, TValue>(IAsyncMemoryCacheConfiguration<TKey, TValue> configuration, ILogger<AsyncMemoryCache<TKey, TValue>> logger)
		where TKey : notnull
		where TValue : IAsyncDisposable
	{
		logger.LogTrace("Starting evictionbehavior - expiry check interval {interval}.", _timer.Period);
		_workerTask = Task.Factory.StartNew(async () =>
		{
			try
			{
				while (await _timer!.WaitForNextTickAsync(_cts.Token) && !_cts.IsCancellationRequested)
				{
					await CheckExpiredItems(configuration, logger);
				}
			}
			catch (OperationCanceledException)
			{
				logger.LogTrace("CancellationToken was cancelled.");
			}
		}, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();

		logger.LogTrace("Stopping behavior.");
	}

	private static async Task CheckExpiredItems<TKey, TValue>(IAsyncMemoryCacheConfiguration<TKey, TValue> configuration, ILogger<AsyncMemoryCache<TKey, TValue>> logger)
		where TKey : notnull
		where TValue : IAsyncDisposable
	{
		logger.LogTrace("Checking expired items");
		var expiredItems = new List<CacheEntity<TKey, TValue>>();

		var cache = configuration.CacheBackingStore;
		foreach (var item in cache.Values)
		{
			if (Interlocked.Decrement(ref item.References) >= 0)
			{
				// Need to increment again to restore the refcounter
				_ = Interlocked.Increment(ref item.References);
				continue;
			}

			if (DateTimeOffset.UtcNow > item.AbsoluteExpiration
				|| DateTimeOffset.UtcNow - item.LastUse > item.SlidingExpiration)
			{
				expiredItems.Add(item);
			}
		}

		foreach (var expiredItem in expiredItems)
		{
			logger.LogTrace("Expiring item with key {key}", expiredItem.Key);
			var item = await expiredItem.ObjectFactory;
			if (cache.Remove(expiredItem.Key) && configuration.CacheItemExpired is not null)
			{
				configuration.CacheItemExpired.Invoke(expiredItem.Key, item);
			}

			await item.DisposeAsync();
		}

		logger.LogTrace("Done checking expired items. Evicted {evictedItemsCount} items.", expiredItems.Count);
	}

	public async ValueTask DisposeAsync()
	{
		_timer.Dispose();
		_cts.Cancel();

		if (_workerTask is not null)
		{
			await _workerTask;
		}

		_cts.Dispose();
	}
}
