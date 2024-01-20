using Nito.AsyncEx;
using System;
using System.Threading.Tasks;

namespace AsyncMemoryCache;

public sealed class CacheEntity<T> where T : IAsyncDisposable
{
	public CacheEntity(string key, Func<Task<T>> objectFactory, AsyncLazyFlags lazyFlags)
	{
		Key = key;
		ObjectFactory = new AsyncLazy<T>(objectFactory, lazyFlags);
	}

	internal DateTime Created { get; } = DateTime.UtcNow;

	public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(30);
	public string Key { get; }
	public AsyncLazy<T> ObjectFactory { get; }
}
