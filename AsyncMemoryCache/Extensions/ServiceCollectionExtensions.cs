using Microsoft.Extensions.DependencyInjection;
using System;

namespace AsyncMemoryCache.Extensions;

public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds the default <see cref="IAsyncMemoryCache{TKey, TValue}"/> implementation and related services to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <typeparam name="TKey">The type of the key to be used for each cache item.</typeparam>
	/// <typeparam name="TValue">The type of the value which will be cached</typeparam>
	/// <param name="services">The <see cref="IServiceCollection"/>.</param>
	/// <returns>The <see cref="IServiceCollection"/>.</returns>
	public static IServiceCollection AddAsyncMemoryCache<TKey, TValue>(this IServiceCollection services)
		where TKey : notnull
		where TValue : IAsyncDisposable
	{
		return services
				.AddSingleton<IAsyncMemoryCache<TKey, TValue>, AsyncMemoryCache<TKey, TValue>>()
				.AddSingleton<IAsyncMemoryCacheConfiguration<TKey, TValue>, AsyncMemoryCacheConfiguration<TKey, TValue>>();
	}

	/// <summary>
	/// Adds the default <see cref="IAsyncMemoryCache{TKey, TValue}"/> implementation and related services to the <see cref="IServiceCollection"/> with a custom <see cref="IAsyncMemoryCacheConfiguration{TKey, TValue}"/> object.
	/// </summary>
	/// <typeparam name="TKey">The type of the key to be used for each cache item.</typeparam>
	/// <typeparam name="TValue">The type of the value which will be cached</typeparam>
	/// <param name="services">The <see cref="IServiceCollection"/>.</param>
	/// <param name="configuration">The custom configuration object.</param>
	/// <returns>The <see cref="IServiceCollection"/></returns>
	public static IServiceCollection AddAsyncMemoryCache<TKey, TValue>(this IServiceCollection services, IAsyncMemoryCacheConfiguration<TKey, TValue> configuration)
		where TKey : notnull
		where TValue : IAsyncDisposable
	{
		return services
				.AddSingleton<IAsyncMemoryCache<TKey, TValue>, AsyncMemoryCache<TKey, TValue>>()
				.AddSingleton(configuration);
	}
}
