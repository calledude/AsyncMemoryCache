![Build](https://img.shields.io/github/actions/workflow/status/calledude/AsyncMemoryCache/build.yml)
![Version](https://img.shields.io/nuget/v/AsyncMemoryCache)
![Downloads](https://img.shields.io/nuget/dt/AsyncMemoryCache)
![License](https://img.shields.io/github/license/calledude/AsyncMemoryCache)

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
var cacheEntityReference = cache.Add("theKey", async () =>
{
	var createdObject = await ...;
	await Task.Delay(1000);
	return createdObject;
});

cacheEntityReference.CacheEntity.WithSlidingExpiration(TimeSpan.FromHours(12));

// Will block here until the object is created
var theCachedObject = await cache["theKey"].CacheEntity.ObjectFactory;
```

### Extending the lifetime of a cached object
#### Given that
- A cache item exists that is expired
- Above mentioned cache item is currently referenced by some code

The way you deal with this is by `using` (calling `Dispose()` on) the `CacheEntityReference` returned whenever the cache object is accessed
As long as at least one `CacheEntityReference` is alive (i.e. not disposed and still in-scope) the underlying cached object will not be evicted/disposed

```cs
var cacheEntityReference = cache.Add("theKey", async () =>
{
	var createdObject = await ...;
	await Task.Delay(1000);
	return createdObject;
});

// Object expires in 15 seconds
cacheEntityReference.CacheEntity.WithAbsoluteExpiration(DateTimeOffset.UtcNow.AddSeconds(15));

// do stuff with cache entry that takes longer than 15 seconds

	...

// The underlying cached object is not immediately disposed here, but is now eligible for disposal later on by eviction behaviors (if enabled)
cacheEntityReference.Dispose();
```
