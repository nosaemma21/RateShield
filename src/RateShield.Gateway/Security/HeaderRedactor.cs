namespace RateShield.Gateway.Security;

public static class HeaderRedactor
{
    private const string RedactedValue = "[REDACTED]";

    private static readonly HashSet<string> SensitiveHeaderNames = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-Api-Key",
        "X-Client-Secret",
        "X-Auth-Token",
    };

    //redact function
    public static string Redact(string headerName, string headerValue)
    {
        if (SensitiveHeaderNames.Contains(headerName))
        {
            return RedactedValue;
        }

        return headerValue;
    }

    public static bool IsSensitive(string headerName)
    {
        return SensitiveHeaderNames.Contains(headerName);
    }
}
