## AsyncMemoryCache
#### A highly configurable cache that aims to improve upon IMemoryCache without relying on it as its backing store.

- Lightweight
- Strongly typed
- Configurable eviction behavior
- Configurable lifetime for cache entries
- Lazy construction of cache object
- Supports asynchronous factories
- Automatic disposal of expired cache entries
- Integration with Microsoft.Extensions.DependencyInjection
- Integration with Microsoft.Extensions.Logging


### Usage
```cs
using AsyncMemoryCache;

ILoggerFactory loggerFactory = ...;

var cacheLogger = loggerFactory.CreateLogger<AsyncMemoryCache<string, TheClassToCache>>();

var cacheConfig = new AsyncMemoryCacheConfiguration<string, TheClassToCache>
{
	EvictionBehavior = EvictionBehavior.Default // new DefaultEvictionBehavior(TimeProvider.System, TimeSpan.FromSeconds(45))
};

var cache = new AsyncMemoryCache<string, TheClassToCache>(cacheConfig, cacheLogger); // Logger is optional

// The factory is started here, will not block
var cacheEntity = cache.Add("theKey", async () =>
{
	var createdObject = await ...;
	await Task.Delay(1000);
	return createdObject;
})
.WithSlidingExpiration(TimeSpan.FromHours(12));

// Will block here until the object is created
var theCachedObject = await cache["theKey"];
```