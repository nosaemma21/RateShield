namespace RateShield.Core.Configuration;

public sealed class IdentityOptions
{
    public string Strategy { get; init; } = "HeaderThenIp";
    public string ApiKeyHeaderName { get; init; } = "X-Api-Key";
    public string ClientIdHeaderName { get; init; } = "X-Client-Id";
    public bool TrustForwardedHeaders { get; init; }
    public string ForwardedForHeaderName { get; set; } = "X-Forwarded-For";
    public string[] TrustedProxyIpAddresses { get; init; } = [];
}
