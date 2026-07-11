using System.Net;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.Options;
using RateShield.Core.Configuration;
using RateShield.Core.Identity;

namespace RateShield.Gateway.Identity;

public sealed class HttpClientIdentityProvider : IClientIdentityProvider<HttpContext>
{
    private const string ApiKeySource = "ApiKeyHeader";
    private const string ClientIdSource = "ClientIdHeader";
    private const string BearerTokenClaimSource = "BearerTokenClaim";
    private const string RemoteIpSource = "RemoteIp";
    private const string ForwardedIpSource = "ForwardedIp";

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

        if (TryGetBearerTokenClaim(context, out var bearerTokenClientId))
        {
            return new ClientIdentity(Value: bearerTokenClientId, Source: BearerTokenClaimSource);
        }

        if (TryGetTrustedForwardedIp(context, out var forwardedIp))
        {
            return new ClientIdentity(Value: forwardedIp, Source: ForwardedIpSource);
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

    //helper for forwarded ip trust
    private bool TryGetTrustedForwardedIp(HttpContext context, out string forwardedIp)
    {
        forwardedIp = string.Empty;

        if (!_options.Identity.TrustForwardedHeaders)
        {
            return false;
        }

        var remoteIp = context.Connection.RemoteIpAddress;

        if (remoteIp is null || !IsTrustedProxy(remoteIp))
        {
            return false;
        }

        if (
            !TryGetHeaderValue(
                context,
                _options.Identity.ForwardedForHeaderName,
                out var forwardedFor
            )
        )
        {
            return false;
        }

        var firstForwardedIp = forwardedFor.Split(',')[0].Trim();

        if (!IPAddress.TryParse(firstForwardedIp, out _))
        {
            return false;
        }

        forwardedIp = firstForwardedIp;
        return true;
    }

    //helper
    //if false,RateShield ignores X-forwarded-for
    private bool IsTrustedProxy(IPAddress remoteIp)
    {
        foreach (var trustedProxy in _options.Identity.TrustedProxyIpAddresses)
        {
            if (!IPAddress.TryParse(trustedProxy, out var trustedProxyIp))
            {
                continue;
            }
            if (remoteIp.Equals(trustedProxyIp))
            {
                return true;
            }
        }

        return false;
    }

    //helper
    private bool TryGetBearerTokenClaim(HttpContext context, out string value)
    {
        value = string.Empty;

        if (string.IsNullOrWhiteSpace(_options.Identity.BearerTokenClientClaimName))
        {
            return false;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var claim = context.User.Claims.FirstOrDefault(claim =>
            string.Equals(
                claim.Type,
                _options.Identity.BearerTokenClientClaimName,
                StringComparison.Ordinal
            )
        );

        if (string.IsNullOrWhiteSpace(claim?.Value))
        {
            return false;
        }

        value = claim.Value.Trim();
        return true;
    }
}
