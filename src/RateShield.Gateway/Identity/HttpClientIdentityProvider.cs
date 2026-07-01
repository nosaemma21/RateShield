using Microsoft.Extensions.Options;
using RateShield.Core.Configuration;
using RateShield.Core.Identity;

namespace RateShield.Gateway.Identity;

public sealed class HttpClientIdentityProvider : IClientIdentityProvider<HttpContext>
{
    private const string ApiKeySource = "ApiKeyHeader";
    private const string ClientIdSource = "ClientIdHeader";
    private const string RemoteIpSource = "RemoteIp";

    private readonly RateShieldOptions _options;

    public HttpClientIdentityProvider(IOptions<RateShieldOptions> options)
    {
        _options = options.Value;
    }

    public ClientIdentity ResolveClient(HttpContext context)
    {
        //   throw new NotImplementedException();
        if (TryGetHeaderValue(context, _options.Identity.ApiKeyHeaderName, out var apiKey))
        {
            return new ClientIdentity(Value: apiKey, Source: ApiKeySource);
        }

        if (TryGetHeaderValue(context, _options.Identity.ClientIdHeaderName, out var clientId))
        {
            return new ClientIdentity(Value: clientId, Source: ClientIdSource);
        }

        var remoteIp = context.Connection.RemoteIpAddress?.ToString();

        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            return new ClientIdentity(Value: remoteIp, Source: RemoteIpSource);
        }

        return new ClientIdentity(Value: "unknown", Source: RemoteIpSource);
    }

    // getting the header value
    private static bool TryGetHeaderValue(HttpContext context, string headerName, out string value)
    {
        value = string.Empty;

        if (string.IsNullOrWhiteSpace(headerName))
        {
            return false;
        }

        if (!context.Request.Headers.TryGetValue(headerName, out var values))
        {
            return false;
        }

        var firstValue = values.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(firstValue))
        {
            return false;
        }

        value = firstValue.Trim();
        return true;
    }
}
