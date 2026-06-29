namespace RateShield.Core.Configuration;

public sealed class RateLimitPolicyOptins
{
    public int Capacity { get; init; } = 100;
    public int RefillTokens { get; init; } = 10;
    public int RefillPeriodSeconds { get; init; } = 1;
    public int RequestCost { get; set; } = 1;
}
