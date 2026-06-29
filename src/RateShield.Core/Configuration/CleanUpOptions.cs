namespace RateShield.Core.Configuration;

// maps to config
public sealed class CleanUpOptions
{
    public int IntervalSeconds { get; init; } = 60;
    public int BucketIdleTimeoutSeconds { get; init; } = 900;
    public int MaxBucketsPerScan { get; init; } = 10_000;
}
