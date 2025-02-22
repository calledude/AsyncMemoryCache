using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncMemoryCache.CreationBehaviors;

internal static class CacheItemFactoryInvoker
{
#if !NET8_0_OR_GREATER
	public static void InvokeFactory<TKey, TValue>(CacheEntity<TKey, TValue> item, ICreationTimeProvider creationTimeProvider)
		where TKey : notnull
		where TValue : IAsyncDisposable
	{
		_ = Task.Factory.StartNew(static async state =>
		{
			var (cacheItem, timeToWait) = ((CacheEntity<TKey, TValue>, TimeSpan))state!;
			if (timeToWait > TimeSpan.Zero)
			{
				await Task.Delay(timeToWait).ConfigureAwait(false);
			}

			cacheItem.ObjectFactory.Start();
		},
		(item, creationTimeProvider.GetExpectedCreationTime() - DateTimeOffset.UtcNow),
		CancellationToken.None,
		TaskCreationOptions.None,
		SynchronizationContext.Current is null
			? TaskScheduler.Default
			: TaskScheduler.FromCurrentSynchronizationContext());
	}
#else
	public static void InvokeFactory<TKey, TValue>(CacheEntity<TKey, TValue> item, ICreationTimeProvider creationTimeProvider, TimeProvider timeProvider)
		where TKey : notnull
		where TValue : IAsyncDisposable
	{
		_ = Task.Factory.StartNew(static async state =>
		{
			var (cacheItem, timeToWait, timeProvider) = ((CacheEntity<TKey, TValue>, TimeSpan, TimeProvider))state!;
			if (timeToWait > TimeSpan.Zero)
			{
				await Task.Delay(timeToWait, timeProvider).ConfigureAwait(false);
			}

			cacheItem.ObjectFactory.Start();
		},
		(item, creationTimeProvider.GetExpectedCreationTime() - timeProvider.GetUtcNow(), timeProvider),
		CancellationToken.None,
		TaskCreationOptions.None,
		SynchronizationContext.Current is null
			? TaskScheduler.Default
			: TaskScheduler.FromCurrentSynchronizationContext());
	}
#endif
}
