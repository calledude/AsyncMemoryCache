﻿using Nito.AsyncEx;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AsyncMemoryCache.Tests;

public class CacheEntityReferenceTests
{
	[Fact]
	public void IncrementsAndDecrementsProperly()
	{
		var cacheEntity = new CacheEntity<string, IAsyncDisposable>("", () => Task.FromResult((IAsyncDisposable)null!), AsyncLazyFlags.None);
		Assert.Equal(0, cacheEntity.Uses);

		var cacheEntityReference = new CacheEntityReference<string, IAsyncDisposable>(cacheEntity);
		Assert.Equal(1, cacheEntity.Uses);

		cacheEntityReference.Dispose();
		Assert.Equal(0, cacheEntity.Uses);
	}

	[Fact]
	public void MultipleDisposes_DecrementsOnlyOnce()
	{
		var cacheEntity = new CacheEntity<string, IAsyncDisposable>("", () => Task.FromResult((IAsyncDisposable)null!), AsyncLazyFlags.None);
		Assert.Equal(0, cacheEntity.Uses);

		var cacheEntityReference = new CacheEntityReference<string, IAsyncDisposable>(cacheEntity);
		Assert.Equal(1, cacheEntity.Uses);
#pragma warning disable
		cacheEntityReference.Dispose();
		cacheEntityReference.Dispose();
		cacheEntityReference.Dispose();
		cacheEntityReference.Dispose();
		cacheEntityReference.Dispose();
#pragma warning enable
		Assert.Equal(0, cacheEntity.Uses);
	}
}
