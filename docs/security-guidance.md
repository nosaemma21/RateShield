# Security Guidance

This guide captures production security decisions for request size limits, upstream timeouts, CORS, and backend TLS.

## Request Body Size

RateShield should protect backend services from unexpectedly large request bodies.

Large bodies can increase:

```text
memory pressure
connection time
upstream processing cost
attack surface for abuse
```

For production, choose explicit body size limits based on the routes RateShield protects.

Examples:

```text
small JSON APIs: 1 MB to 5 MB
file upload routes: route-specific larger limit
unknown public APIs: keep conservative defaults
```

Do not use one large global limit just because one route needs uploads.

Prefer route-specific limits when upload and non-upload routes share the same gateway.

## Slow Upstream Services

RateShield should not wait forever for a backend response.

Slow upstreams can hold gateway connections open and reduce capacity for healthy traffic.

Production deployments should configure timeouts for proxied traffic.

Timeout decisions should consider:

```text
normal backend latency
long-running endpoint behavior
client retry behavior
rate-limit policy strictness
hosting platform timeout limits
```

For normal API routes, prefer a bounded timeout rather than an infinite wait.

For long-running operations, prefer async job patterns instead of keeping a gateway request open for a long time.

## CORS Decision

CORS is only needed when browser-based clients call RateShield directly from a different origin.

If RateShield is used by:

```text
server-to-server clients
mobile apps
CLI clients
same-origin web apps
```

then CORS may not be needed.

If browser clients are expected, configure an explicit allow-list of origins.

Avoid production CORS settings like:

```text
AllowAnyOrigin
AllowAnyHeader
AllowAnyMethod
```

unless the API is intentionally public and does not rely on browser credentials.

For credentialed browser requests, never combine wildcard origins with credentials.

## Backend TLS

Production backend destinations should use HTTPS whenever traffic leaves a trusted private network.

Use HTTP only when the backend connection is protected by the hosting platform or private network boundary.

For hosted deployments, confirm:

```text
whether backend traffic stays inside a private network
whether the backend destination supports HTTPS
whether TLS certificates are valid and trusted
whether internal service URLs are exposed publicly
```

Do not disable certificate validation in production.

If a backend uses a private certificate authority, configure the runtime or container trust store intentionally instead of bypassing TLS checks.

## Production Checklist

Before production, confirm:

```text
request body limits are intentional
slow upstream timeout behavior is documented
CORS is disabled or explicitly allow-listed
backend destinations use HTTPS when required
TLS certificate validation remains enabled
```
## Bearer Token Claim Identity

Bearer token claim identity is deferred for a later auth-aware version of RateShield.

Current RateShield identity extraction supports:

- API key header
- configured client ID header
- trusted forwarded IP
- remote IP fallback

A future bearer token provider can extract identity from validated JWT claims such as `sub`, `client_id`, or `azp`.

RateShield should only trust bearer token claims after authentication middleware has validated the token signature, issuer, audience, and expiry. The rate limiter must not parse unvalidated JWTs and treat their claims as trusted identity.