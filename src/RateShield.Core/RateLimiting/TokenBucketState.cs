namespace RateShield.Core.RateLimiting;

/// <summary>
/// Current state of the client tokens
/// </summary>
public sealed class TokenBucketState
{
    public TokenBucketState(
        int availableTokens,
        DateTimeOffset lastRefilledAt,
        DateTimeOffset lastSeenAt
    )
    {
        AvailableTokens = availableTokens;
        LastRefilledAt = lastRefilledAt;
        LastSeenAt = lastSeenAt;
    }

    public int AvailableTokens { get; set; }
    public DateTimeOffset LastRefilledAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
}
