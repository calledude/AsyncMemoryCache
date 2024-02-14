using AsyncMemoryCache.ExpirationStrategy;
using System;
using Xunit;

namespace AsyncMemoryCache.Tests;

public class ExpirationStrategyTests
{
	[Theory]
	[InlineData(-1, true)]
	[InlineData(1, false)]
	public void AbsoluteExpirationStrategy(int minuteOffset, bool expectedExpiryState)
	{
		var target = new AbsoluteExpirationStrategy(DateTimeOffset.UtcNow.AddMinutes(minuteOffset));
		Assert.Equal(expectedExpiryState, target.IsExpired());
	}

	[Theory]
	[InlineData(-2, true)]
	[InlineData(0, false)]
	public void SlidingExpirationStrategy(int lastUseOffset, bool expectedExpiryState)
	{
		var target = new SlidingExpirationStrategy(TimeSpan.FromMinutes(1))
		{
			LastUse = DateTimeOffset.UtcNow.AddMinutes(lastUseOffset)
		};

		Assert.Equal(expectedExpiryState, target.IsExpired());
	}

	[Fact]
	public void SlidingExpirationStrategy_CacheEntityAccessed_SetsLastUse()
	{
		var lastUse = DateTimeOffset.MinValue;
		var target = new SlidingExpirationStrategy(TimeSpan.Zero)
		{
			LastUse = lastUse
		};

		target.CacheEntityAccessed();

		Assert.NotEqual(lastUse, target.LastUse);
	}
}
