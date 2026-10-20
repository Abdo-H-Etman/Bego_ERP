using System.Collections.Concurrent;
using System.Text.Json;
using Erp.Infrastructure.Abstractions.Caching;
using Microsoft.Extensions.Configuration;
using ILogger = Serilog.ILogger;

namespace Erp.Api.Caching;

public class RedisCacheService(ILogger logger) : IRedisCacheService
{
    private readonly ILogger _logger = logger.ForContext<RedisCacheService>();
    private static readonly ConcurrentDictionary<string, (DateTime Expiry, string Json)> FallbackStore = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (FallbackStore.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow < entry.Expiry)
            {
                var val = JsonSerializer.Deserialize<T>(entry.Json);
                return Task.FromResult(val);
            }

            FallbackStore.TryRemove(key, out _);
        }

        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var expiration = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromHours(1));
        var json = JsonSerializer.Serialize(value);
        FallbackStore[key] = (expiration, json);
        _logger.Debug("Cache set for key: {Key} with expiry {Expiry}", key, expiration);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        FallbackStore.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var keysToRemove = FallbackStore.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var k in keysToRemove)
        {
            FallbackStore.TryRemove(k, out _);
        }

        return Task.CompletedTask;
    }
}
