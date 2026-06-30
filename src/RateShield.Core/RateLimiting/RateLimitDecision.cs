namespace RateShield.Core.RateLimiting;

public sealed record RateLimitDecision(
    bool IsAllowed,
    string PolicyName,
    int Limit,
    int Remaining,
    DateTimeOffset ResetAt,
    TimeSpan? RetryAfter
)
{
    // shape of allowed request
    public static RateLimitDecision Allowed(
        string policyName,
        int limit,
        int remaining,
        DateTimeOffset resetAt
    )
    {
        return new RateLimitDecision(
            IsAllowed: true,
            PolicyName: policyName,
            Limit: limit,
            Remaining: remaining,
            ResetAt: resetAt,
            RetryAfter: null
        );
    }

    // shape of rejected request
    public static RateLimitDecision Rejected(
        string policyName,
        int limit,
        int remaining,
        DateTimeOffset resetAt,
        TimeSpan retryAfter
    )
    {
        return new RateLimitDecision(
            IsAllowed: false,
            PolicyName: policyName,
            Limit: limit,
            Remaining: remaining,
            ResetAt: resetAt,
            RetryAfter: retryAfter
        );
    }
}
