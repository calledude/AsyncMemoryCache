using Microsoft.Extensions.DependencyInjection;

namespace AsyncMemoryCache;

public static class Extensions
{
	public static IServiceCollection AddAsyncMemoryCache(this IServiceCollection services)
		=> services
			.AddSingleton(typeof(IAsyncMemoryCache<>), typeof(AsyncMemoryCache<>))
			.AddSingleton(EvictionBehavior.Default);
}
