using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using Xunit;

namespace AsyncMemoryCache.Tests;

public class ExtensionsTests
{
	[Fact]
	public void CanResolveAsyncMemoryCacheWithLogger()
	{
		var logger = NullLoggerFactory.Instance.CreateLogger<AsyncMemoryCache<IAsyncDisposable>>();
		var serviceProvider = new ServiceCollection()
			.AddAsyncMemoryCache()
			.AddSingleton(logger)
			.BuildServiceProvider();

		var asyncMemoryCache = serviceProvider.GetService<IAsyncMemoryCache<IAsyncDisposable>>();
		Assert.NotNull(asyncMemoryCache);
	}

	[Fact]
	public void CanResolveAsyncMemoryCacheWithoutLogger()
	{
		var serviceProvider = new ServiceCollection()
			.AddAsyncMemoryCache()
			.BuildServiceProvider();

		var asyncMemoryCache = serviceProvider.GetService<IAsyncMemoryCache<IAsyncDisposable>>();
		Assert.NotNull(asyncMemoryCache);
	}
}
