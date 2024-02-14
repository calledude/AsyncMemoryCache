using System;

namespace AsyncMemoryCache.ExpirationStrategy;

internal sealed class SlidingExpirationStrategy : IExpirationStrategy
{
	internal DateTimeOffset LastUse { get; set; }

	internal TimeSpan SlidingExpirationWindow { get; }

	internal SlidingExpirationStrategy(TimeSpan slidingExpirationWindow)
	{
		SlidingExpirationWindow = slidingExpirationWindow;
		LastUse = DateTimeOffset.UtcNow;
	}

	public bool IsExpired()
		=> DateTimeOffset.UtcNow - LastUse > SlidingExpirationWindow;

	public void CacheEntityAccessed()
		=> LastUse = DateTimeOffset.UtcNow;
}