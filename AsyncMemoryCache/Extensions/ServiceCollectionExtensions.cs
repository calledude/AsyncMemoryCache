using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace AsyncMemoryCache.Extensions;

/// <summary>
/// A class containing extension methods to help with configuration of an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
	extension(IServiceCollection services)
	{
		/// <summary>
		/// Adds the default <see cref="IAsyncMemoryCache{TKey, TValue}"/> implementation and related services to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <typeparam name="TKey">The type of the key to be used for each cache item.</typeparam>
		/// <typeparam name="TValue">The type of the value which will be cached</typeparam>
		/// <returns>The <see cref="IServiceCollection"/>.</returns>
		public IServiceCollection AddAsyncMemoryCache<TKey, TValue>()
			where TKey : notnull
			where TValue : IAsyncDisposable
		{
			return services
				.AddSingleton<IAsyncMemoryCache<TKey, TValue>>(
					sp => AsyncMemoryCache<TKey, TValue>.Create(
						sp.GetRequiredService<IAsyncMemoryCacheConfiguration<TKey, TValue>>(),
						sp.GetService<ILogger<AsyncMemoryCache<TKey, TValue>>>()))
				.AddSingleton<IAsyncMemoryCacheConfiguration<TKey, TValue>, AsyncMemoryCacheConfiguration<TKey, TValue>>();
		}

		/// <summary>
		/// Adds the default <see cref="IAsyncMemoryCache{TKey, TValue}"/> implementation and related services to the <see cref="IServiceCollection"/> with a custom <see cref="IAsyncMemoryCacheConfiguration{TKey, TValue}"/> object.
		/// </summary>
		/// <typeparam name="TKey">The type of the key to be used for each cache item.</typeparam>
		/// <typeparam name="TValue">The type of the value which will be cached</typeparam>
		/// <param name="configuration">The custom configuration object.</param>
		/// <returns>The <see cref="IServiceCollection"/></returns>
		public IServiceCollection AddAsyncMemoryCache<TKey, TValue>(IAsyncMemoryCacheConfiguration<TKey, TValue> configuration)
			where TKey : notnull
			where TValue : IAsyncDisposable
		{
			return services
				.AddSingleton<IAsyncMemoryCache<TKey, TValue>>(
					sp => AsyncMemoryCache<TKey, TValue>.Create(
						configuration,
						sp.GetService<ILogger<AsyncMemoryCache<TKey, TValue>>>()));
		}
	}
}
