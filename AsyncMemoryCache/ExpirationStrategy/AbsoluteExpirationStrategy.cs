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

	[ExcludeFromCodeCoverage(Justification = "Empty implementation")]
	public void CacheEntityAccessed() { }
}
