namespace RateShield.Core.RateLimiting;

public interface ITokenBucketStore
{
    TokenBucketState GetOrCreate(TokenBucketKey key, DateTimeOffset createdAt, int capacity);

    int Count { get; }

    IReadOnlyCollection<TokenBucketKey> GetIdleKeys(DateTimeOffset idleBefore, int maxKeys);

    bool TryRemove(TokenBucketKey key);
}
