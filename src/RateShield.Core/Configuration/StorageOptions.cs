namespace RateShield.Core.Configuration;

// corresponds to storage config string
public sealed class StorageOptions
{
    public string Mode { get; init; } = "InMemory";
    public string FailureBehavior { get; init; } = "FailClosed";
}
