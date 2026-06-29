namespace RateShield.Core.Configuration;

// maps to the config
public sealed class RejectionResponseOptions
{
    public string ContentType { get; set; } = "application/json";
    public string ErrorCode { get; init; } = "rate_limit_exceeded";
    public string Message { get; init; } = "Too many requests.";
}
