using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncMemoryCache.EvictionBehaviors;

/// <summary>
/// The default provided <see cref="IEvictionBehavior"/>.
/// This class perodically checks for expired items and evicts them. Handling disposal and eviction callbacks as needed.
/// <para/>
/// If a cache entity is considered to be expired but still has a live reference via a <see cref="CacheEntityReference{TKey, TValue}"/> object the underlying cached item will not be evicted from the cache.
/// </summary>
public sealed class DefaultEvictionBehavior : IEvictionBehavior
{
	private readonly CancellationTokenSource _cts;
	private readonly TimeSpan _interval;
#if NET8_0_OR_GREATER
	private readonly PeriodicTimer _timer;
	private Task? _workerTask;

	/// <summary>
	/// Creates a new instance of <see cref="DefaultEvictionBehavior"/> with the supplied arguments.
	/// </summary>
	/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
	/// <param name="evictionCheckInterval">The interval to use when checking for expired items.</param>
	public DefaultEvictionBehavior(TimeProvider? timeProvider = default, TimeSpan? evictionCheckInterval = default)
	{
		_interval = evictionCheckInterval ?? TimeSpan.FromSeconds(30);
		_timer = new(_interval, timeProvider ?? TimeProvider.System);
		_cts = new();
	}
#else
	private Timer? _timer;

	/// <summary>
	/// Creates a new instance of <see cref="DefaultEvictionBehavior"/> with the supplied arguments.
	/// </summary>
	/// <param name="evictionCheckInterval">The interval to use when checking for expired items.</param>
	public DefaultEvictionBehavior(TimeSpan? evictionCheckInterval = default)
	{
		_cts = new();
		_interval = evictionCheckInterval ?? TimeSpan.FromSeconds(30);
	}
#endif

	/// <inheritdoc/>
	public void Start<TKey, TValue>(IAsyncMemoryCacheConfiguration<TKey, TValue> configuration, ILogger<AsyncMemoryCache<TKey, TValue>> logger)
		where TKey : notnull
		where TValue : IAsyncDisposable
	{
		logger.LogTrace("Starting evictionbehavior - expiry check interval {Interval}.", _interval);
#if NET8_0_OR_GREATER
		_workerTask = Task.Factory.StartNew(async () =>
		{
			try
			{
				while (await _timer!.WaitForNextTickAsync(_cts.Token).ConfigureAwait(false) && !_cts.IsCancellationRequested)
				{
					await CheckExpiredItems(configuration, logger).ConfigureAwait(false);
				}
			}
			catch (OperationCanceledException)
			{
				logger.LogTrace("CancellationToken was cancelled.");
			}

			logger.LogTrace("Stopping behavior.");
		}, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default)
		.Unwrap();

#else
		_timer = new(async _ => await CheckExpiredItems(configuration, logger).ConfigureAwait(false), null, 0, (uint)_interval.TotalMilliseconds);
#endif
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

				logger.LogTrace("Keeping expired cache item {Key} because it is still being referenced", item.Key);
				continue;
			}

			if (!(item.ExpirationStrategy?.IsExpired() ?? false))
				continue;

			expiredItems.Add(item);
		}

		foreach (var expiredItem in expiredItems)
		{
			logger.LogTrace("Expiring item with key {Key}", expiredItem.Key);
			var item = await expiredItem.ObjectFactory;
			if (cache.Remove(expiredItem.Key))
			{
				configuration.CacheItemExpired?.Invoke(expiredItem.Key, item);
				expiredItem.ExpirationCallback?.Invoke(expiredItem.Key, item);
			}

			await item.DisposeAsync().ConfigureAwait(false);
		}

		logger.LogTrace("Done checking expired items. Evicted {EvictedItemsCount} items.", expiredItems.Count);
	}

	/// <inheritdoc/>
#if NET8_0_OR_GREATER
	public async ValueTask DisposeAsync()
	{
		_timer.Dispose();
		await _cts.CancelAsync().ConfigureAwait(false);

		if (_workerTask is not null)
		{
			await _workerTask.ConfigureAwait(false);
		}

		_cts.Dispose();
	}
#else
	public ValueTask DisposeAsync()
	{
		_timer?.Dispose();
		_cts.Cancel();
		_cts.Dispose();

		return default;
	}
#endif
}
