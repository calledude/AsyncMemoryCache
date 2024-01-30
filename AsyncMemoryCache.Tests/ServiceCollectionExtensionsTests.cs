using AsyncMemoryCache.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System;
using Xunit;

namespace AsyncMemoryCache.Tests;

public class ServiceCollectionExtensionsTests
{
	[Fact]
	public void CanResolveAsyncMemoryCacheWithLogger()
	{
		var logger = NullLoggerFactory.Instance.CreateLogger<AsyncMemoryCache<string, IAsyncDisposable>>();
		var serviceProvider = new ServiceCollection()
			.AddAsyncMemoryCache<string, IAsyncDisposable>()
			.AddSingleton(logger)
			.BuildServiceProvider();

		var asyncMemoryCache = serviceProvider.GetService<IAsyncMemoryCache<string, IAsyncDisposable>>();
		Assert.NotNull(asyncMemoryCache);
	}

	[Fact]
	public void CanResolveAsyncMemoryCacheWithoutLogger()
	{
		var serviceProvider = new ServiceCollection()
			.AddAsyncMemoryCache<string, IAsyncDisposable>()
			.BuildServiceProvider();

		var asyncMemoryCache = serviceProvider.GetService<IAsyncMemoryCache<string, IAsyncDisposable>>();
		Assert.NotNull(asyncMemoryCache);
	}

	[Fact]
	public void CanResolveAsyncMemoryCache_UsesCustomConfiguration()
	{
		var customConfiguration = Substitute.For<IAsyncMemoryCacheConfiguration<string, IAsyncDisposable>>();

		var serviceProvider = new ServiceCollection()
			.AddAsyncMemoryCache(customConfiguration)
			.BuildServiceProvider();

		var asyncMemoryCache = serviceProvider.GetService<IAsyncMemoryCache<string, IAsyncDisposable>>();
		Assert.NotNull(asyncMemoryCache);

		_ = customConfiguration.Received(1).CacheBackingStore;
		_ = customConfiguration.Received(1).EvictionBehavior;

		customConfiguration.EvictionBehavior.Received(1).Start(customConfiguration, Arg.Any<ILogger<AsyncMemoryCache<string, IAsyncDisposable>>>());
	}
}
