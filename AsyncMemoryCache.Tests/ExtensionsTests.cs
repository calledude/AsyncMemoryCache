using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace AsyncMemoryCache.Tests;

public class ExtensionsTests
{
	[Fact]
	public void CanResolveAsyncMemoryCache()
	{
		var serviceProvider = new ServiceCollection()
			.AddAsyncMemoryCache()
			.BuildServiceProvider();

		var asyncMemoryCache = serviceProvider.GetService<IAsyncMemoryCache<IAsyncDisposable>>();
		Assert.NotNull(asyncMemoryCache);
	}
}
