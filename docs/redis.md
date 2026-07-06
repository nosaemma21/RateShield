# Redis Operation Guide

Redis is the distributed storage backplane for RateShield V2. In Redis mode, gateway instances share token-bucket state so rate limits apply consistently across multiple running RateShield processes.

This guide explains when to use Redis mode, which configuration keys are required, how failures behave, how buckets are stored, and how to test Redis locally.

## When To Use Redis Mode

Use Redis mode when:

- RateShield runs more than one gateway instance.
- Rate-limit state must be shared across horizontally scaled services.
- A single client should have one shared quota across all gateway replicas.
- Production traffic needs consistent limiting instead of per-instance in-memory counters.

Use in-memory mode when:

- Running local development without Redis.
- Running simple single-instance demos.
- Testing code paths that do not need distributed state.

## Required Configuration

Redis mode is enabled through the storage setting:

```json
{
  "RateShield": {
    "Storage": {
      "Mode": "Redis",
      "FailureBehavior": "FailClosed"
    }
  }
}
```

When `RateShield:Storage:Mode` is `Redis`, these Redis settings are required or recommended:

- `RateShield:Redis:ConnectionString`
- `RateShield:Redis:ConnectTimeoutMilliseconds`
- `RateShield:Redis:CommandTimeoutMilliseconds`

Example:

```json
{
  "RateShield": {
    "Storage": {
      "Mode": "Redis",
      "FailureBehavior": "FailClosed"
    },
    "Redis": {
      "ConnectionString": "localhost:6379",
      "ConnectTimeoutMilliseconds": 5000,
      "CommandTimeoutMilliseconds": 1000
    }
  }
}
```

For hosted environments, provide the connection string through environment variables or platform secrets instead of committing it to source control.

## Environment Variables

ASP.NET Core maps double underscores to nested configuration keys.

```text
RateShield__Storage__Mode=Redis
RateShield__Storage__FailureBehavior=FailClosed
RateShield__Redis__ConnectionString=<redis-connection-string>
RateShield__Redis__ConnectTimeoutMilliseconds=5000
RateShield__Redis__CommandTimeoutMilliseconds=1000
```

## Failure Behavior

Redis failures are handled according to `RateShield:Storage:FailureBehavior`.

### FailClosed

`FailClosed` rejects requests when Redis cannot be reached or when Redis evaluation fails.

Use this for protected production routes because it prevents backend services from receiving unbounded traffic when the distributed limiter cannot verify quota state.

### FailOpen

`FailOpen` allows requests when Redis fails.

Use this only for local development, controlled emergency recovery, or availability-first systems where allowing traffic is safer than rejecting it. It can expose backend services to overload during a Redis outage.

## Timeout Settings

`RateShield:Redis:ConnectTimeoutMilliseconds` controls how long RateShield waits while establishing the Redis connection.

`RateShield:Redis:CommandTimeoutMilliseconds` controls how long Redis commands, including the token-bucket Lua script, can run before timing out.

Recommended starting values:

```json
{
  "ConnectTimeoutMilliseconds": 5000,
  "CommandTimeoutMilliseconds": 1000
}
```

Tune these values based on the hosting environment, network latency, Redis provider, and production load testing.

## Redis Key Format

RateShield stores token buckets with keys shaped like:

```text
rateshield:v1:bucket:{routeId}:{policyName}:{clientHash}
```

The raw client identity is not stored in the key. RateShield hashes the identity source and identity value before building the Redis key.

This keeps API keys, custom client IDs, and IP-derived identity values out of Redis key names and logs.

## Stored Bucket Fields

Each bucket is stored as a Redis hash with these fields:

```text
tokens
last_refilled_at_unix_ms
last_seen_at_unix_ms
```

`tokens` is the current available token count.

`last_refilled_at_unix_ms` is the timestamp used to calculate earned refill tokens.

`last_seen_at_unix_ms` records the latest request activity for the bucket.

## Atomic Token-Bucket Updates

RateShield uses a Redis Lua script for token-bucket evaluation.

The Lua script performs these operations atomically:

- Read the current bucket.
- Calculate elapsed refill periods.
- Add earned tokens without exceeding capacity.
- Consume tokens when enough are available.
- Reject when tokens are insufficient.
- Calculate retry-after and reset timestamps.
- Save the updated bucket.
- Refresh the bucket TTL.

Atomic execution matters because multiple gateway instances can receive traffic for the same client at the same time. Redis runs the Lua script as one operation, so two gateways cannot both spend the same token.

## TTL Strategy

Redis buckets receive an expiry based on:

```text
RateShield:Cleanup:BucketIdleTimeoutSeconds
```

Every successful Redis evaluation refreshes the bucket expiry. This removes inactive client buckets automatically and prevents Redis from retaining stale rate-limit state forever.

## Local Testing

Redis integration tests use Testcontainers.

Docker Desktop must be running before executing the full test suite.

Run:

```powershell
dotnet test
```

The tests start a temporary Redis container, run the Redis token-bucket script against it, and dispose of the container after the test run.

## Production Notes

Use a managed Redis-compatible service for hosted production environments.

For Render, use Render Key Value and provide the connection string through Render environment variables or secrets.

Recommended production posture:

- Use `FailClosed` for protected APIs.
- Keep Redis credentials out of source control.
- Use TLS-enabled Redis endpoints when the provider supports or requires them.
- Configure Redis memory and eviction policy intentionally.
- Monitor Redis latency, command errors, memory usage, and connection health.
- Load test multiple RateShield instances against the same Redis backplane before production rollout.

## Troubleshooting

If RateShield fails to start in Redis mode, check:

- `RateShield:Redis:ConnectionString` is configured.
- The Redis host is reachable from the gateway environment.
- Redis credentials are correct.
- TLS requirements match the provider connection string.
- `ConnectTimeoutMilliseconds` and `CommandTimeoutMilliseconds` are greater than `0`.

If requests are unexpectedly rejected, check:

- `RateShield:Storage:FailureBehavior`.
- Redis connectivity and provider health.
- Redis command timeout settings.
- The configured policy capacity, refill period, refill tokens, and request cost.

If rate limits do not appear shared across gateway instances, check:

- All instances use `RateShield:Storage:Mode=Redis`.
- All instances use the same Redis connection string.
- Route IDs and policy names match across deployments.
- Client identity extraction is configured consistently across instances.

## Production Redis Sizing Notes

Redis memory usage depends on the number of active client-route-policy buckets.

A rough sizing model is:

```text
active_buckets = unique_clients * protected_routes_per_client