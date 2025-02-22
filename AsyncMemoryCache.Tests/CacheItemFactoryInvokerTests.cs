using AsyncMemoryCache.CreationBehaviors;
using Microsoft.Extensions.Time.Testing;
using Nito.AsyncEx;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AsyncMemoryCache.Tests
{
	public class CacheItemFactoryInvokerTests
	{
		[Fact]
		public void InvokeFactory_ShouldInvokeObjectFactoryAfterDelay()
		{
			using var syncContext = new SingleThreadSynchronizationContext();

			var manualResetEvent = new ManualResetEvent(false);

			var factory = () =>
			{
				manualResetEvent.Set();
				return Task.FromResult(Substitute.For<IAsyncDisposable>());
			};

			var cacheEntity = new CacheEntity<string, IAsyncDisposable>("test", factory, AsyncLazyFlags.None);

			var creationTimeProvider = Substitute.For<ICreationTimeProvider>();
			var timeProvider = new FakeTimeProvider();
			var expectedCreationTime = timeProvider.GetUtcNow().AddMilliseconds(100);
			creationTimeProvider.GetExpectedCreationTime().Returns(expectedCreationTime);

			CacheItemFactoryInvoker.InvokeFactory(cacheEntity, creationTimeProvider, timeProvider);

			syncContext.RunOnCurrentThread();

			Assert.False(cacheEntity.ObjectFactory.IsStarted);
			timeProvider.Advance(TimeSpan.FromMilliseconds(99));
			Assert.False(cacheEntity.ObjectFactory.IsStarted);
			timeProvider.Advance(TimeSpan.FromMilliseconds(100));

			manualResetEvent.WaitOne();
			Assert.True(cacheEntity.ObjectFactory.IsStarted);
		}

		[Fact]
		public void InvokeFactory_ShouldInvokeObjectFactoryImmediatelyIfNoDelay()
		{
			using var syncContext = new SingleThreadSynchronizationContext();

			var manualResetEvent = new ManualResetEvent(false);

			var factory = () =>
			{
				manualResetEvent.Set();
				return Task.FromResult(Substitute.For<IAsyncDisposable>());
			};

			var cacheEntity = new CacheEntity<string, IAsyncDisposable>("test", factory, AsyncLazyFlags.None);

			var sw = Stopwatch.StartNew();
			CacheItemFactoryInvoker.InvokeFactory(cacheEntity, CreationTimeProvider.Default, TimeProvider.System);

			syncContext.RunOnCurrentThread();

			manualResetEvent.WaitOne();
			Assert.InRange(sw.ElapsedMilliseconds, 0, 50);
		}

		private class SingleThreadSynchronizationContext : SynchronizationContext, IDisposable
		{
			private readonly Queue<(SendOrPostCallback, object?)> _queue = new();
			private readonly SynchronizationContext? _originalContext;

			public SingleThreadSynchronizationContext()
			{
				_originalContext = Current;
				SetSynchronizationContext(this);
			}

			public override void Post(SendOrPostCallback d, object? state)
			{
				_queue.Enqueue((d, state));
			}

			public void RunOnCurrentThread()
			{
				while (_queue.Count > 0)
				{
					var (callback, state) = _queue.Dequeue();
					callback(state);
				}
			}

			public void Dispose()
			{
				SetSynchronizationContext(_originalContext);
			}
		}
	}
}
