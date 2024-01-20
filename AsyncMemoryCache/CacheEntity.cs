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

	public DateTime Created { get; } = DateTime.UtcNow;
	public string Key { get; }
	public AsyncLazy<T> ObjectFactory { get; }
}
