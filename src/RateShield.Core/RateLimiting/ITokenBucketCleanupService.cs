namespace RateShield.Core.RateLimiting;

public interface ITokenBucketCleanupService
{
    int RemoveIdleBuckets();
}
