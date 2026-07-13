# Gateway Flow

RateShield is an ASP.NET Core reverse-proxy gateway with a token-bucket rate limiter in front of YARP.

The main request flow is:

```text
Client
  -> RateShield.Gateway
  -> ASP.NET Core routing
  -> RateShield rate-limiting middleware
  -> YARP reverse proxy
  -> Backend service
```

## YARP Concepts

YARP is the reverse-proxy library RateShield uses to forward allowed requests to backend services.

YARP has three important concepts:

- Route: decides which incoming requests should be proxied.
- Cluster: a named group of backend destinations.
- Destination: one concrete backend address inside a cluster.

### Route

A YARP route decides which incoming requests should be handled by the proxy.

Example:

```json
"sample-api": {
  "ClusterId": "sample-backend",
  "Match": {
    "Path": "/api/{**catch-all}"
  }
}
```

This means requests under `/api` match the `sample-api` route.

Examples:

```text
/api
/api/hello
/api/users/123
/api/hello?source=gateway
```

The route ID is important because RateShield uses it to choose the rate-limit policy.

### Cluster

A YARP cluster is a named group of backend destinations.

Example:

```json
"sample-backend": {
  "Destinations": {
    "sample-backend-primary": {
      "Address": "http://localhost:5255/"
    }
  }
}
```

The route points to this cluster with:

```json
"ClusterId": "sample-backend"
```

### Destination

A destination is one concrete backend address inside a cluster.

For local development:

```text
Gateway:        http://localhost:5011
SampleBackend:  http://localhost:5255
```

So this request:

```text
http://localhost:5011/api/hello
```

is forwarded to:

```text
http://localhost:5255/api/hello
```

YARP preserves the original path by default.

The YARP destination must point to the backend, not the gateway.

Correct:

```json
"Address": "http://localhost:5255/"
```

Incorrect:

```json
"Address": "http://localhost:5011/"
```

Pointing YARP to the gateway would create a proxy loop.

## Path Transform Decision

RateShield does not use YARP path transforms by default.

The gateway preserves the incoming request path when forwarding to the backend. This keeps routing predictable and makes RateShield behave like a protective gateway in front of existing services instead of a URL-rewriting layer.

Example:

```text
Client -> RateShield: /api/orders/123
RateShield -> Backend: /api/orders/123
```

Path transforms can be added later for specific routes if a backend expects a different path shape, such as removing an external `/api` prefix before forwarding.

## Host Header Decision

RateShield does not preserve the original `Host` header by default.

By default, YARP sends the destination host to the backend. This means the backend receives a `Host` value that matches the configured backend destination instead of the public gateway address.

Example:

```text
Client -> RateShield: Host: localhost:5011
RateShield -> Backend: Host: localhost:5255
```

This is the safest default for RateShield because it avoids surprising backend host validation behavior and keeps local, Docker, and hosted routing predictable.

Preserving the original `Host` header can be enabled later for specific backend services that require the public gateway host for tenant resolution, absolute URL generation, or strict host-based routing.

## HTTP Version Decision

RateShield does not force a specific upstream HTTP version by default.

YARP is allowed to use the normal HTTP version behavior supported by .NET and the configured backend destination. This keeps local development, Docker Compose, and simple backend services compatible without extra protocol requirements.

The gateway can configure an explicit upstream HTTP version later if a production backend requires it, such as HTTP/2 for gRPC or a service mesh.

## RateShield Policy Mapping

YARP config decides where requests go.

RateShield config decides whether requests are allowed.

Example:

```json
"RateShield": {
  "Routes": {
    "sample-api": {
      "PolicyName": "Default"
    }
  }
}
```

This means:

```text
YARP route sample-api uses RateShield policy Default.
```

This separation is intentional:

- YARP decides where requests go.
- RateShield decides whether requests are allowed before forwarding.

## Policy Selection Order

RateShield selects a rate-limit policy using the matched YARP route ID.

The order is:

1. ASP.NET Core routing matches the incoming request to a YARP route.
2. RateShield reads the matched YARP route ID.
3. RateShield checks `RateShield:Routes:{routeId}:PolicyName`.
4. If the route has a configured policy, that named policy is used.
5. If the route has no specific policy mapping, RateShield falls back to the `Default` policy.
6. If the selected policy does not exist, startup validation should fail.

Example:

```json
"RateShield": {
  "Routes": {
    "sample-api": {
      "PolicyName": "Strict"
    }
  },
  "Policies": {
    "Default": {
      "Capacity": 100,
      "RefillTokens": 10,
      "RefillPeriodSeconds": 1,
      "RequestCost": 1
    },
    "Strict": {
      "Capacity": 20,
      "RefillTokens": 2,
      "RefillPeriodSeconds": 1,
      "RequestCost": 1
    }
  }
}
```

In this example, requests matched to the YARP route `sample-api` use the `Strict` policy.

## Middleware Order

The gateway pipeline is ordered like this:

```csharp
app.UseRouting();

app.UseRateShieldRateLimiting();

app.MapRateShieldEndpoints();
```

`UseRouting()` must run before the rate-limiting middleware because the middleware needs endpoint metadata to find the matched YARP route ID.

The rate-limiting middleware only limits proxied YARP traffic.

These endpoints bypass rate limiting:

```text
/
/health/live
/health/ready
```

They are gateway endpoints, not backend proxy routes. This keeps platform health checks reliable and prevents gateway diagnostics from consuming client quota.

## Rate-Limiting Flow

For a proxied request:

```text
1. Resolve YARP route ID.
2. Resolve client identity.
3. Resolve the RateShield policy for the route.
4. Build a token bucket key from client ID, route ID, and policy name.
5. Get or create the bucket.
6. Refill earned tokens.
7. Consume tokens if available.
8. Allow the request to YARP or reject with HTTP 429.
```

Allowed responses include:

```text
X-RateLimit-Limit
X-RateLimit-Remaining
X-RateLimit-Reset
```

Rejected responses include:

```text
HTTP 429 Too Many Requests
Retry-After
X-RateLimit-Limit
X-RateLimit-Remaining
X-RateLimit-Reset
```

### Rate-Limit Header Compatibility Decision

RateShield keeps the existing `X-RateLimit-Limit`, `X-RateLimit-Remaining`, and
`X-RateLimit-Reset` fields for compatibility with clients that already
understand the common de facto convention. Rejected requests also include the
standard `Retry-After` field.

RateShield does not currently emit `RateLimit-Limit`, `RateLimit-Remaining`, or
`RateLimit-Reset`. Those names were defined by older Internet-Drafts and did not
become a finalized HTTP standard. The active May 2026 IETF draft instead defines
the combined `RateLimit` and `RateLimit-Policy` fields, and remains a work in
progress. RateShield should revisit adoption after the specification is
published as an RFC so its public contract is not tied to a superseded draft.

References:

- [Current IETF RateLimit header-fields draft](https://datatracker.ietf.org/doc/draft-ietf-httpapi-ratelimit-headers/)
- [RFC 6585: 429 Too Many Requests](https://www.rfc-editor.org/rfc/rfc6585.html#section-4)

## Current Limitations

- `X-Forwarded-For` is not trusted yet.
- Redis distributed storage is not implemented yet.
- Observability metrics are not fully implemented yet.
- YARP transforms are not configured yet because local forwarding currently preserves the path as needed.

## Destination Health Check Decision

RateShield enables YARP passive health checks for backend destinations.

Passive health checks observe real proxied traffic. If a backend destination starts failing transport-level requests, YARP can mark that destination as unhealthy and temporarily avoid sending traffic to it.

RateShield does not enable YARP active health checks by default yet.

Active health checks require YARP to send background probe requests to each backend destination. That is useful when all protected backends expose a consistent health endpoint, but RateShield should not assume that every backend has the same health path.

The default decision is:

```text
Passive health checks: enabled
Active health checks: deferred
```

## Unavailable Destination Decision

RateShield treats unavailable backend destinations as upstream availability failures, not rate-limit failures.

If YARP has no usable backend destination for a matched route, the gateway should return `503 Service Unavailable`. RateShield should not convert this into `429 Too Many Requests` because the client did not exceed its quota.

This keeps operational signals clear:

```text
429 Too Many Requests -> client exceeded rate limit
503 Service Unavailable -> backend destination unavailable
```
