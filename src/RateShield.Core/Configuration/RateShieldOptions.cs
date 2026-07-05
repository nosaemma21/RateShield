namespace RateShield.Core.Configuration;

// for typed config
public sealed class RateShieldOptions
{
    public const string SectionName = "RateShield";
    public StorageOptions Storage { get; init; } = new();
    public IdentityOptions Identity { get; init; } = new();
    public CleanUpOptions CleanUp { get; init; } = new();
    public RejectionResponseOptions RejectionResponse { get; init; } = new();
    public Dictionary<string, RateLimitPolicyOptins> Policies { get; init; } = new();
    public Dictionary<string, RoutePolicyOptions> Routes { get; init; } = new();
    public ObservabilityOptions Observability { get; init; } = new();
    public RedisOptions Redis { get; init; } = new();
}
