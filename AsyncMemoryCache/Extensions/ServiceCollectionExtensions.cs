using Microsoft.Extensions.DependencyInjection;
using System;

namespace AsyncMemoryCache.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddAsyncMemoryCache<TKey, TValue>(this IServiceCollection services)
		where TKey : notnull
		where TValue : IAsyncDisposable
	{
		return services
				.AddSingleton<IAsyncMemoryCache<TKey, TValue>, AsyncMemoryCache<TKey, TValue>>()
				.AddSingleton<IAsyncMemoryCacheConfiguration<TKey, TValue>, AsyncMemoryCacheConfiguration<TKey, TValue>>();
	}

	public static IServiceCollection AddAsyncMemoryCache<TKey, TValue>(this IServiceCollection services, IAsyncMemoryCacheConfiguration<TKey, TValue> configuration)
		where TKey : notnull
		where TValue : IAsyncDisposable
	{
		return services
				.AddSingleton<IAsyncMemoryCache<TKey, TValue>, AsyncMemoryCache<TKey, TValue>>()
				.AddSingleton(configuration);
	}
}
