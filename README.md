![Build](https://img.shields.io/github/actions/workflow/status/calledude/AsyncMemoryCache/build.yml)
[![Version](https://img.shields.io/nuget/v/AsyncMemoryCache)](https://www.nuget.org/packages/AsyncMemoryCache)
[![Coverage](https://codecov.io/gh/calledude/AsyncMemoryCache/graph/badge.svg)](https://codecov.io/gh/calledude/AsyncMemoryCache)
[![CodeFactor](https://img.shields.io/codefactor/grade/github/calledude/AsyncMemoryCache)](https://www.codefactor.io/repository/github/calledude/asyncmemorycache/)
[![Downloads](https://img.shields.io/nuget/dt/AsyncMemoryCache)](https://www.nuget.org/packages/AsyncMemoryCache)
[![License](https://img.shields.io/github/license/calledude/AsyncMemoryCache)](https://github.com/calledude/AsyncMemoryCache/blob/master/LICENSE)

## AsyncMemoryCache
#### A highly configurable cache that aims to improve upon IMemoryCache without relying on it as its backing store.

- Lightweight
- Strongly typed
- Configurable eviction behavior
- Configurable lifetime for cache entries
- Lazy construction of cache object
- Supports asynchronous factories
- Automatic disposal of expired cache entries
- Custom creation time providers
- (Optional) Integration with Microsoft.Extensions.DependencyInjection
- (Optional) Integration with Microsoft.Extensions.Logging


### Usage
```cs
using AsyncMemoryCache;
using AsyncMemoryCache.Extensions;

ILoggerFactory loggerFactory = ...;

var cacheLogger = loggerFactory.CreateLogger<AsyncMemoryCache<string, TheClassToCache>>();

var cacheConfig = new AsyncMemoryCacheConfiguration<string, TheClassToCache>
{
	EvictionBehavior = EvictionBehavior.Default // new DefaultEvictionBehavior(TimeProvider.System, TimeSpan.FromSeconds(45))
};

var cache = AsyncMemoryCache<string, TheClassToCache>.Create(cacheConfig, cacheLogger); // Logger is optional

// The factory is started here, will not block
var cacheEntityReference = cache.GetOrCreate("theKey", async () =>
{
	var createdObject = await ...;
	await Task.Delay(1000);
	return createdObject;
})
.WithSlidingExpiration(TimeSpan.FromHours(12));

// Will block here until the object is created
var theCachedObject = await cache["theKey"]; // Short-hand for await cache["theKey"].CacheEntity.ObjectFactory;
```

### Extending the lifetime of a cached object
#### Given that
- A cache item exists that is expired
- Above mentioned cache item is currently referenced by some code

The way you deal with this is by `using` (calling `Dispose()` on) the `CacheEntityReference` returned whenever the cache object is accessed
As long as at least one `CacheEntityReference` is alive (i.e. not disposed and still in-scope) the underlying cached object will not be evicted/disposed

```cs
// Create a cache entry that expires in 15 seconds
var cacheEntityReference = cache.GetOrCreate("theKey", async () =>
{
	var createdObject = await ...;
	await Task.Delay(1000);
	return createdObject;
})
.WithAbsoluteExpiration(DateTimeOffset.UtcNow.AddSeconds(15));

// do stuff with cache entry that takes longer than 15 seconds

	...

// The underlying cached object is not immediately disposed here, but is now eligible for disposal later on by eviction behaviors (if enabled)
cacheEntityReference.Dispose();
```

### Custom Creation Time Providers
You can provide custom creation time providers to control the expected creation time of cache entries.

```cs
public class CustomCreationTimeProvider : ICreationTimeProvider
{
	public DateTimeOffset GetExpectedCreationTime()
	{
		// Custom logic to determine the creation time
		return DateTimeOffset.UtcNow.AddMinutes(5);
	}
}

var customCreationTimeProvider = new CustomCreationTimeProvider();
var cacheEntityReference = cache.GetOrCreate("theKey", factory, creationTimeProvider: customCreationTimeProvider);
```