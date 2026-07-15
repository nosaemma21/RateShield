# Configuration Reference

RateShield uses the standard ASP.NET Core configuration system. Configuration is
loaded from `appsettings.json`, the active environment file, environment
variables, and command-line arguments. Sources loaded later override earlier
sources.

The application-specific settings are nested under the `RateShield` section.
In JSON, keys use colons conceptually; in environment variables, replace each
colon with a double underscore.

```text
RateShield:Storage:Mode
RateShield__Storage__Mode
```

Environment variable names are case-insensitive on Windows but may be
case-sensitive on other platforms. Use the exact casing shown in this guide.

## Storage

| JSON key | Environment variable | Type | Default | Description |
| --- | --- | --- | --- | --- |
| `RateShield:Storage:Mode` | `RateShield__Storage__Mode` | string | `InMemory` | Selects the limiter state store. Supported modes are `InMemory` and `Redis`. Use Redis when multiple gateway instances must share limits. |
| `RateShield:Storage:FailureBehavior` | `RateShield__Storage__FailureBehavior` | string | `FailClosed` | Controls the decision returned when Redis evaluation fails. `FailClosed` rejects protected requests; `FailOpen` permits them. Keep `FailClosed` for protected production routes. |

`FailureBehavior` is relevant to Redis mode. In-memory evaluation does not
depend on an external storage service.

## Identity

RateShield checks authenticated bearer claims, configured headers, and client IP
information to create a stable limiter identity. See
[trusted-proxies.md](trusted-proxies.md) for the forwarded-IP trust model.

| JSON key | Environment variable | Type | Default | Description |
| --- | --- | --- | --- | --- |
| `RateShield:Identity:Strategy` | `RateShield__Identity__Strategy` | string | `HeaderThenIp` | Names the configured identity strategy. It must not be empty. |
| `RateShield:Identity:ApiKeyHeaderName` | `RateShield__Identity__ApiKeyHeaderName` | string | `X-Api-Key` | Header checked for API-key identity. It must not be empty. Header values are used for identity calculation but are redacted from logs. |
| `RateShield:Identity:ClientIdHeaderName` | `RateShield__Identity__ClientIdHeaderName` | string | `X-Client-Id` | Header checked for a custom client identity. It must not be empty. |
| `RateShield:Identity:BearerTokenClientClaimName` | `RateShield__Identity__BearerTokenClientClaimName` | string | `sub` | Claim used when the request already has an authenticated bearer identity, such as `sub` or `client_id`. |
| `RateShield:Identity:TrustForwardedHeaders` | `RateShield__Identity__TrustForwardedHeaders` | boolean | `false` | Enables client-IP extraction from the configured forwarded-for header. Enable only behind known proxies. |
| `RateShield:Identity:ForwardedForHeaderName` | `RateShield__Identity__ForwardedForHeaderName` | string | `X-Forwarded-For` | Forwarded client-IP header. It is required when forwarded headers are trusted. |
| `RateShield:Identity:TrustedProxyIpAddresses` | `RateShield__Identity__TrustedProxyIpAddresses__0`, `__1`, etc. | string array | empty | Exact proxy IP addresses permitted to supply the forwarded-for header. At least one is required when `TrustForwardedHeaders` is `true`. |

Example array configuration:

```text
RateShield__Identity__TrustForwardedHeaders=true
RateShield__Identity__TrustedProxyIpAddresses__0=10.0.0.10
RateShield__Identity__TrustedProxyIpAddresses__1=10.0.0.11
```

## Cleanup

Cleanup removes idle buckets from the in-memory store. Redis bucket expiry is
handled with Redis TTLs, using the idle timeout as an input.

| JSON key | Environment variable | Type | Default | Validation and behavior |
| --- | --- | --- | --- | --- |
| `RateShield:Cleanup:IntervalSeconds` | `RateShield__Cleanup__IntervalSeconds` | integer | `60` | Seconds between cleanup runs. Must be greater than zero. |
| `RateShield:Cleanup:BucketIdleTimeoutSeconds` | `RateShield__Cleanup__BucketIdleTimeoutSeconds` | integer | `900` | A bucket older than this idle period is eligible for removal. Must be greater than zero. |
| `RateShield:Cleanup:MaxBucketsPerScan` | `RateShield__Cleanup__MaxBucketsPerScan` | integer | `10000` | Maximum bucket keys examined in one cleanup run. Must be greater than zero. |

## Rejection Response

These settings control the safe response produced when RateShield rejects a
request. Internal policy and storage details are not included.

| JSON key | Environment variable | Type | Default | Description |
| --- | --- | --- | --- | --- |
| `RateShield:RejectionResponse:ContentType` | `RateShield__RejectionResponse__ContentType` | string | `application/json` | Response content type. It must not be empty. |
| `RateShield:RejectionResponse:ErrorCode` | `RateShield__RejectionResponse__ErrorCode` | string | `rate_limit_exceeded` | Stable machine-readable error code. It must not be empty. |
| `RateShield:RejectionResponse:Message` | `RateShield__RejectionResponse__Message` | string | `Too many requests.` | Safe client-facing message. It must not be empty. |

## Policies

Policies are a dictionary. Replace `{policyName}` with a configured name such as
`Default`, `Strict`, or `Premium`. At least one policy named exactly `Default`
is required.

| JSON key | Environment variable pattern | Type | Default | Validation and behavior |
| --- | --- | --- | --- | --- |
| `RateShield:Policies:{policyName}:Capacity` | `RateShield__Policies__{policyName}__Capacity` | integer | `100` | Maximum tokens in the bucket. Must be greater than zero. |
| `RateShield:Policies:{policyName}:RefillTokens` | `RateShield__Policies__{policyName}__RefillTokens` | integer | `10` | Tokens added after each refill period. Must be greater than zero. |
| `RateShield:Policies:{policyName}:RefillPeriodSeconds` | `RateShield__Policies__{policyName}__RefillPeriodSeconds` | integer | `1` | Length of one refill period. Must be greater than zero. |
| `RateShield:Policies:{policyName}:RequestCost` | `RateShield__Policies__{policyName}__RequestCost` | integer | `1` | Tokens consumed per request. Must be greater than zero and no greater than `Capacity`. |

Example:

```text
RateShield__Policies__Default__Capacity=100
RateShield__Policies__Default__RefillTokens=10
RateShield__Policies__Default__RefillPeriodSeconds=1
RateShield__Policies__Default__RequestCost=1
```

## Routes

Route policy mappings are a dictionary keyed by YARP route ID. Replace
`{routeId}` with the exact ID under `ReverseProxy:Routes`.

| JSON key | Environment variable pattern | Type | Default | Validation and behavior |
| --- | --- | --- | --- | --- |
| `RateShield:Routes:{routeId}:PolicyName` | `RateShield__Routes__{routeId}__PolicyName` | string | `Default` | Policy applied to the matching YARP route. It must name an existing RateShield policy. Routes without an explicit mapping fall back to `Default`. |

Example:

```text
RateShield__Routes__sample-api__PolicyName=Strict
```

## Add A New Rate-Limit Policy

A policy defines the token-bucket behavior. A route mapping decides which YARP
route uses that policy. Adding a policy without mapping a route to it does not
change request behavior.

The example below adds an `Uploads` policy for a YARP route whose exact route ID
is `upload-api`.

### 1. Choose The Bucket Values

For this example:

```text
Capacity            = 20
RefillTokens         = 5
RefillPeriodSeconds  = 60
RequestCost          = 1
```

This allows an initial burst of 20 requests. Afterward, the bucket earns five
tokens every 60 seconds. Because each request costs one token, the long-term
refill allowance is five requests per minute. Increasing `RequestCost` makes
each request consume more of the same bucket capacity.

### 2. Add The Policy And Route Mapping

Insert the new entries into the existing `Policies` and `Routes` dictionaries
under `RateShield`. Do not create a second top-level `RateShield` section.

```json
{
  "RateShield": {
    "Policies": {
      "Uploads": {
        "Capacity": 20,
        "RefillTokens": 5,
        "RefillPeriodSeconds": 60,
        "RequestCost": 1
      }
    },
    "Routes": {
      "upload-api": {
        "PolicyName": "Uploads"
      }
    }
  }
}
```

`upload-api` must exactly match a key under `ReverseProxy:Routes`. Policy and
route names are configuration identifiers; keep their spelling and casing
consistent.

If the YARP route already exists, only the RateShield policy and mapping are
needed. If it does not exist, also define the YARP route and its cluster:

```json
{
  "ReverseProxy": {
    "Routes": {
      "upload-api": {
        "ClusterId": "sample-backend",
        "Match": {
          "Path": "/api/uploads/{**catch-all}"
        }
      }
    }
  }
}
```

When route patterns overlap, configure YARP route precedence intentionally so
the request matches the expected route ID. RateShield applies the policy for
the route selected by YARP.

### 3. Use Environment Variables In Hosted Environments

The same policy and mapping can be supplied without changing JSON:

```text
RateShield__Policies__Uploads__Capacity=20
RateShield__Policies__Uploads__RefillTokens=5
RateShield__Policies__Uploads__RefillPeriodSeconds=60
RateShield__Policies__Uploads__RequestCost=1
RateShield__Routes__upload-api__PolicyName=Uploads
```

If the YARP route is also new, its environment-variable form is:

```text
ReverseProxy__Routes__upload-api__ClusterId=sample-backend
ReverseProxy__Routes__upload-api__Match__Path=/api/uploads/{**catch-all}
```

### 4. Restart And Verify

RateShield binds and validates policy configuration at startup, so restart or
redeploy the gateway after changing these values. Startup fails if a route
mapping names a policy that does not exist or if a policy value is invalid.

Use one stable test identity and send more requests than the configured
capacity:

```powershell
1..25 | ForEach-Object {
    curl.exe -s -o NUL -w "%{http_code}`n" `
        -H "X-Client-Id: uploads-policy-test" `
        http://localhost:8080/api/uploads/example
}
```

For a fresh bucket, the first 20 rapid requests should return HTTP 200 and later
requests should return HTTP 429 until tokens refill. Inspect a response with
`curl.exe -i` and confirm that `X-RateLimit-Limit` reports `20`; rejected
responses should also contain `Retry-After`.

Use a new client ID when repeating the test immediately, especially in Redis
mode, so an existing bucket does not change the expected starting capacity.

## Observability

| JSON key | Environment variable | Type | Default | Description |
| --- | --- | --- | --- | --- |
| `RateShield:Observability:Enabled` | `RateShield__Observability__Enabled` | boolean | `true` | Enables RateShield metrics registration. |
| `RateShield:Observability:MetricsExporter` | `RateShield__Observability__MetricsExporter` | string | `Prometheus` | Selects the configured metrics export path. The current supported value is `Prometheus`. |

See [observability.md](observability.md) for metric names, labels, and the
Prometheus endpoint.

## Redis

Redis settings must be nested under `RateShield:Redis`. They are required only
when `RateShield:Storage:Mode` is `Redis`.

| JSON key | Environment variable | Type | Default | Validation and behavior |
| --- | --- | --- | --- | --- |
| `RateShield:Redis:ConnectionString` | `RateShield__Redis__ConnectionString` | string | empty | Redis connection string. It is required in Redis mode and must be supplied through a secret store in hosted environments. |
| `RateShield:Redis:ConnectTimeoutMilliseconds` | `RateShield__Redis__ConnectTimeoutMilliseconds` | integer | `5000` | Maximum initial connection wait. Must be greater than zero in Redis mode. |
| `RateShield:Redis:CommandTimeoutMilliseconds` | `RateShield__Redis__CommandTimeoutMilliseconds` | integer | `1000` | Maximum duration for Redis operations such as the Lua token-bucket script. Must be greater than zero in Redis mode. |

Never commit a real Redis connection string. See [secrets.md](secrets.md) and
[redis.md](redis.md) for deployment and operational guidance.

## Complete JSON Example

```json
{
  "RateShield": {
    "Storage": {
      "Mode": "Redis",
      "FailureBehavior": "FailClosed"
    },
    "Identity": {
      "Strategy": "HeaderThenIp",
      "ApiKeyHeaderName": "X-Api-Key",
      "ClientIdHeaderName": "X-Client-Id",
      "BearerTokenClientClaimName": "sub",
      "TrustForwardedHeaders": false,
      "ForwardedForHeaderName": "X-Forwarded-For",
      "TrustedProxyIpAddresses": []
    },
    "Cleanup": {
      "IntervalSeconds": 60,
      "BucketIdleTimeoutSeconds": 900,
      "MaxBucketsPerScan": 10000
    },
    "RejectionResponse": {
      "ContentType": "application/json",
      "ErrorCode": "rate_limit_exceeded",
      "Message": "Too many requests."
    },
    "Policies": {
      "Default": {
        "Capacity": 100,
        "RefillTokens": 10,
        "RefillPeriodSeconds": 1,
        "RequestCost": 1
      }
    },
    "Routes": {
      "sample-api": {
        "PolicyName": "Default"
      }
    },
    "Observability": {
      "Enabled": true,
      "MetricsExporter": "Prometheus"
    },
    "Redis": {
      "ConnectionString": "<supply-through-a-secret-store>",
      "ConnectTimeoutMilliseconds": 5000,
      "CommandTimeoutMilliseconds": 1000
    }
  }
}
```

## Startup Validation

RateShield validates required settings during startup. Invalid policy values,
unknown route policy names, invalid cleanup values, incomplete rejection
responses, unsafe forwarded-header configuration, and missing Redis settings in
Redis mode prevent startup. Treat validation failures as configuration errors;
do not bypass them in production.

## Related Platform Configuration

RateShield also relies on standard ASP.NET Core and YARP configuration:

- `ASPNETCORE_ENVIRONMENT` selects the active environment file.
- `ASPNETCORE_URLS` controls the addresses Kestrel listens on.
- `Logging` controls ASP.NET Core log levels and console output.
- `ReverseProxy:Routes` and `ReverseProxy:Clusters` configure YARP routing and
  upstream destinations.

See [gateway-flow.md](gateway-flow.md) for YARP concepts and
[render-deployment.md](render-deployment.md) for hosted values.
