using System.Collections.Concurrent;
using RateShield.Core.RateLimiting;

namespace RateShield.Infrastructure.RateLimiting;

public sealed class InMemoryTokenBucketStore : ITokenBucketStore
{
    private readonly ConcurrentDictionary<TokenBucketKey, TokenBucketState> _buckets = new();

    public int Count => _buckets.Count;

    public IReadOnlyCollection<TokenBucketKey> GetIdleKeys(DateTimeOffset idleBefore, int maxKeys)
    {
        return _buckets
            .Where(pair => pair.Value.LastSeenAt < idleBefore)
            .Take(maxKeys)
            .Select(pair => pair.Key)
            .ToArray(); //linq
    }

    public TokenBucketState GetOrCreate(TokenBucketKey key, DateTimeOffset createdAt, int capacity)
    {
        return _buckets.GetOrAdd(
            key,
            _ => new TokenBucketState(
                availableTokens: capacity,
                lastRefilledAt: createdAt,
                lastSeenAt: createdAt
            )
        );
    }

    public bool TryRemove(TokenBucketKey key)
    {
        //   throw new NotImplementedException();
        return _buckets.TryRemove(key, out _);
    }
}
