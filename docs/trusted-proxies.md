# Trusted Proxies

RateShield can use `X-Forwarded-For` to identify the original client IP when the gateway is running behind a trusted reverse proxy or load balancer.

## Why This Matters

`X-Forwarded-For` is only a request header.

That means a client can fake it:

```http
X-Forwarded-For: 1.2.3.4
```

RateShield must not trust that value unless the request came from a proxy that is explicitly trusted.

## Identity Priority

RateShield resolves client identity in this order:

```text
API key header
client ID header
trusted forwarded IP
remote IP
unknown fallback
```

`X-Forwarded-For` is only used when:

```text
RateShield:Identity:TrustForwardedHeaders = true
```

and the direct remote IP is listed in:

```text
RateShield:Identity:TrustedProxyIpAddresses
```

## Local Example

```json
{
  "RateShield": {
    "Identity": {
      "TrustForwardedHeaders": true,
      "ForwardedForHeaderName": "X-Forwarded-For",
      "TrustedProxyIpAddresses": [
        "127.0.0.1",
        "::1"
      ]
    }
  }
}
```

## Production Guidance

In production, only add proxy IPs that belong to your hosting provider, load balancer, ingress controller, or CDN.

Do not add broad public ranges unless you fully control that network path.

Do not trust `X-Forwarded-For` directly from the public internet.

## Render Note

On Render, HTTPS terminates at Render's edge before traffic reaches the service.

For production, confirm the direct proxy IP behavior from Render before enabling forwarded header trust.

Until the trusted proxy list is confirmed, keep:

```json
"TrustForwardedHeaders": false
```

## Safe Default

The safe default is:

```json
"TrustForwardedHeaders": false
```

When this is false, RateShield ignores `X-Forwarded-For` and uses the direct remote IP fallback.
