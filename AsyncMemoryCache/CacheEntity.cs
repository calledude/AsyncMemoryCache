using Nito.AsyncEx;
using System;
using System.Threading.Tasks;

namespace AsyncMemoryCache;

public sealed class CacheEntity<T> where T : IAsyncDisposable
{
	public CacheEntity(string key, Func<Task<T>> objectFactory)
	{
		// TODO: Configurable lazy flags?

		Key = key;
		ObjectFactory = new AsyncLazy<T>(objectFactory, AsyncLazyFlags.None);
	}

	internal DateTime Created { get; } = DateTime.UtcNow;

	public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(30);
	public string Key { get; }
	public AsyncLazy<T> ObjectFactory { get; }
}
