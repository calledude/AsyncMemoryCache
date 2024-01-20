using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncMemoryCache;

public static class EvictionBehavior
{
	public static readonly IEvictionBehavior Default = new DefaultEvictionBehavior(TimeProvider.System);
	public static readonly IEvictionBehavior Disabled = new NoOpEvictionBehavior();
}

public interface IEvictionBehavior : IAsyncDisposable
{
	void Start<T>(IDictionary<string, CacheEntity<T>> cache, AsyncMemoryCacheConfiguration<T> configuration) where T : IAsyncDisposable;
}

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

	public void Start<T>(IDictionary<string, CacheEntity<T>> cache, AsyncMemoryCacheConfiguration<T> configuration) where T : IAsyncDisposable
	{
		_workerTask = Task.Factory.StartNew(async () =>
		{
			try
			{
				while (await _timer!.WaitForNextTickAsync(_cts.Token) && !_cts.IsCancellationRequested)
				{
					await CheckExpiredItems(cache, configuration);
				}
			}
			catch (OperationCanceledException)
			{
			}
		}, TaskCreationOptions.LongRunning).Unwrap();
	}

	private static async Task CheckExpiredItems<T>(IDictionary<string, CacheEntity<T>> cache, AsyncMemoryCacheConfiguration<T> configuration) where T : IAsyncDisposable
	{
		var expiredItems = new List<CacheEntity<T>>();

		foreach (var item in cache.Values)
		{
			if (DateTime.UtcNow - item.Created > item.Lifetime)
			{
				expiredItems.Add(item);
			}
		}

		foreach (var expiredItem in expiredItems)
		{
			var item = await expiredItem.ObjectFactory;
			if (cache.Remove(expiredItem.Key) && configuration.CacheItemExpired is not null)
			{
				configuration.CacheItemExpired.Invoke(expiredItem.Key, item);
			}

			await item.DisposeAsync();
		}
	}

	public async ValueTask DisposeAsync()
	{
		_cts.Cancel();

		if (_workerTask is not null)
		{
			await _workerTask;
		}

		_cts.Dispose();
	}
}

internal sealed class NoOpEvictionBehavior : IEvictionBehavior
{
	public void Start<T>(IDictionary<string, CacheEntity<T>> cache, AsyncMemoryCacheConfiguration<T> configuration) where T : IAsyncDisposable { }
	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}