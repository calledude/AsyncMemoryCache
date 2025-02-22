using System;

namespace AsyncMemoryCache.CreationBehaviors;

/// <summary>
/// Provides the current time as the expected creation time.
/// </summary>
public class ImmediateCreationTimeProvider : ICreationTimeProvider
{
    /// <summary>
    /// Gets the expected creation time, which is the current time.
    /// </summary>
    /// <returns>The current <see cref="DateTimeOffset"/>.</returns>
    public DateTimeOffset GetExpectedCreationTime() => DateTimeOffset.Now;
}
