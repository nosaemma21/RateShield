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

    /// <summary>
    /// The number of tokens currently available for this bucket.
    /// Requests can be allowed only when this value is greater than or equal to the request cost.
    /// </summary>
    public int AvailableTokens { get; set; }

    /// <summary>
    /// The timestamp of the last completed refill calculation.
    /// This is used to determine how many refill periods have elapsed since the bucket was updated.
    /// </summary>
    public DateTimeOffset LastRefilledAt { get; set; }

    /// <summary>
    /// The timestamp of the most recent request seen for this bucket.
    /// Cleanup workers use this value to remove buckets that have been idle for too long.
    /// </summary>
    public DateTimeOffset LastSeenAt { get; set; }
}
