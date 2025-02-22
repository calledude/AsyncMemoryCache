using System;

namespace AsyncMemoryCache.CreationBehaviors;

/// <summary>
/// A class containing default values for implementations of <see cref="ICreationTimeProvider"/>.
/// </summary>
public static class CreationTimeProvider
{
    /// <inheritdoc cref="ImmediateCreationTimeProvider"/>
    public static readonly ICreationTimeProvider Default = new ImmediateCreationTimeProvider();
}

/// <summary>
/// An interface that can be used to implement custom creation time providers.
/// See <see cref="CreationTimeProvider"/> for default implementations."/>
/// </summary>
public interface ICreationTimeProvider
{
    /// <summary>
    /// Gets the expected creation time.
    /// </summary>
    /// <returns>The expected creation time as a <see cref="DateTimeOffset"/>.</returns>
    DateTimeOffset GetExpectedCreationTime();
}
