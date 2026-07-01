using System.Collections.Concurrent;
using RateShield.Core.RateLimiting;

namespace RateShield.Infrastructure.RateLimiting;

public sealed class TokenBucketLockProvider
{
    private readonly ConcurrentDictionary<TokenBucketKey, object> _locks = new();

    // 1 bucket key per locked object
    public object GetLock(TokenBucketKey key)
    {
        return _locks.GetOrAdd(key, _ => new object());
    }
}
