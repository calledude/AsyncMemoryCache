using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncMemoryCache;

public abstract class EvictionBehavior : IAsyncDisposable
{
	public static readonly EvictionBehavior Default = new DefaultEvictionBehavior(TimeProvider.System);
	public static readonly EvictionBehavior Disabled = new NoOpEvictionBehavior();

	public abstract ValueTask DisposeAsync();
	internal abstract void Start<T>(WeakReference<AsyncMemoryCache<T>> cacheRef) where T : IAsyncDisposable;
}

public sealed class DefaultEvictionBehavior : EvictionBehavior
{
	private readonly PeriodicTimer _timer;
	private readonly CancellationTokenSource _cts;
	private Task<Task>? _workerTask;

	public DefaultEvictionBehavior(TimeProvider? timeProvider = default, TimeSpan? evictionCheckInterval = default)
	{
		var interval = evictionCheckInterval ?? TimeSpan.FromSeconds(30);
		_timer = new PeriodicTimer(interval, timeProvider ?? TimeProvider.System);
		_cts = new CancellationTokenSource();
	}

	internal override void Start<T>(WeakReference<AsyncMemoryCache<T>> cacheRef)
	{
		_workerTask = Task.Factory.StartNew(async () =>
		{
			try
			{
				while (await _timer!.WaitForNextTickAsync(_cts.Token) && !_cts.IsCancellationRequested && cacheRef.TryGetTarget(out var cache))
				{
					await CheckExpiredItems(cache);
				}
			}
			catch (OperationCanceledException)
			{
			}
		}, TaskCreationOptions.LongRunning);
	}

	private static async Task CheckExpiredItems<T>(AsyncMemoryCache<T> cache) where T : IAsyncDisposable
	{
		var expiredItems = new List<CacheEntity<T>>();

		foreach (var item in cache.Cache.Values)
		{
			if (DateTime.UtcNow - item.Created > cache.CacheItemLifeTime)
			{
				expiredItems.Add(item);
			}
		}

		foreach (var expiredItem in expiredItems)
		{
			var item = await expiredItem.ObjectFactory;
			if (cache.Cache.TryRemove(expiredItem.Key, out _))
			{
				cache.InvokeCacheItemExpiredEvent(expiredItem.Key, item);
			}

			await item.DisposeAsync();
		}
	}

	public override async ValueTask DisposeAsync()
	{
		_cts.Cancel();

		if (_workerTask is not null)
		{
			await _workerTask;
		}

		_cts.Dispose();
	}
}

internal sealed class NoOpEvictionBehavior : EvictionBehavior
{
	internal override void Start<T>(WeakReference<AsyncMemoryCache<T>> cacheRef) { }
	public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
}