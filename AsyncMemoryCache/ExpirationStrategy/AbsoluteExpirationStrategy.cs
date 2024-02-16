using System;
using System.Diagnostics.CodeAnalysis;

namespace AsyncMemoryCache.ExpirationStrategy;

internal sealed class AbsoluteExpirationStrategy : IExpirationStrategy
{
	internal DateTimeOffset AbsoluteExpiration { get; }

	internal AbsoluteExpirationStrategy(DateTimeOffset expiryDate)
	{
		AbsoluteExpiration = expiryDate;
	}

	public bool IsExpired()
		=> DateTimeOffset.UtcNow > AbsoluteExpiration;

#if NET8_0_OR_GREATER
	[ExcludeFromCodeCoverage(Justification = "Empty implementation")]
#endif
	public void CacheEntityAccessed()
	{
	}
}
