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

## Managed Redis Setup

For production, RateShield should use a managed Redis-compatible service instead of a Redis container running inside the gateway service.

For Render, use Render Key Value.

Recommended setup:

- Create a Render Key Value instance in the same region as the RateShield gateway.
- Use the internal connection string when RateShield and Redis are in the same Render private network.
- Store the Redis connection string as a Render environment variable or secret.
- Map the connection string to `RateShield__Redis__ConnectionString`.
- Keep `RateShield__Storage__Mode=Redis` for distributed mode.
- Keep `RateShield__Storage__FailureBehavior=FailClosed` for protected production APIs.

Example Render environment variables:

```text
RateShield__Storage__Mode=Redis
RateShield__Storage__FailureBehavior=FailClosed
RateShield__Redis__ConnectionString=<render-key-value-internal-connection-string>
RateShield__Redis__ConnectTimeoutMilliseconds=5000
RateShield__Redis__CommandTimeoutMilliseconds=1000
```

Use the same region for the gateway and Redis to reduce latency.

Do not commit Redis credentials to `appsettings.json`, `.env`, Docker files, or documentation.

## Self-Hosted Redis Container Setup

Self-hosted Redis is useful for local development, demos, and controlled non-production environments. For production, prefer a managed Redis-compatible service unless the team is prepared to operate Redis directly.

A simple local Redis container can be started with:

```powershell
docker run --name rateshield-redis -p 6379:6379 redis:7-alpine
```

Local environment variables:

```text
RateShield__Storage__Mode=Redis
RateShield__Storage__FailureBehavior=FailClosed
RateShield__Redis__ConnectionString=localhost:6379
RateShield__Redis__ConnectTimeoutMilliseconds=5000
RateShield__Redis__CommandTimeoutMilliseconds=1000
```

Self-hosted Redis production responsibilities include:

- persistence configuration
- memory limits
- eviction policy
- authentication
- TLS or private networking
- backups
- monitoring
- patching
- restart strategy
- data recovery planning

Do not run Redis inside the same container as RateShield. Redis should be a separate container, service, or managed dependency.

For local Docker Compose setups, define Redis as a separate service and point RateShield to the Redis service name instead of `localhost`.

## Production Redis Sizing Notes

Redis memory usage depends on the number of active client-route-policy buckets.

A rough sizing model is:

```text
active_buckets = unique_clients * protected_routes_per_client
```

Each bucket stores a Redis key plus a small hash containing token and timestamp fields. Actual memory usage varies by Redis version, key length, allocator behavior, provider overhead, and whether the provider adds metadata around keys.

Start by estimating:

- peak unique clients during the bucket idle timeout window
- number of protected route IDs
- average number of protected routes each client can touch
- configured `BucketIdleTimeoutSeconds`
- Redis provider memory limit
- expected gateway instance count

Example:

```text
50,000 active clients * 3 protected route buckets = 150,000 active Redis buckets
```

Production guidance:

- Set `BucketIdleTimeoutSeconds` low enough to expire inactive clients.
- Avoid unlimited Redis memory growth.
- Use a `noeviction` policy for strict rate limiting when supported.
- Monitor Redis memory usage, key count, latency, and command errors.
- Alert when Redis memory usage exceeds 70-80%.
- Load test expected peak client cardinality before production.
- Keep key names compact, but never store raw API keys or tokens.
- Use larger Redis plans before enabling aggressive gateway autoscaling.
- Revisit Redis sizing whenever route count, client count, or rate-limit policy count changes.

## Redis Persistence Expectations

RateShield Redis state is operational rate-limit state, not business data.

If Redis loses data, existing buckets reset and clients may temporarily receive fresh token buckets. This can briefly allow more traffic than intended, but it does not lose application records.

For most RateShield deployments, Redis persistence is recommended but not treated as the source of truth for business-critical data.

Recommended posture:

- Use managed Redis persistence options when available.
- Prefer persistence for production environments.
- Do not depend on Redis persistence as the only protection against abuse.
- Keep `FailClosed` enabled for protected APIs when Redis is unavailable.
- Treat Redis restore events as limiter reset events.
- Monitor Redis restarts, failovers, and key count drops.

For self-hosted Redis, decide intentionally between:

- RDB snapshots for simpler periodic persistence.
- AOF persistence for stronger durability.
- No persistence only for local development or disposable test environments.

RateShield should remain functional after Redis data loss, but rate-limit counters will restart from fresh buckets.

## Redis Memory Policy

Redis memory must be sized and capped intentionally in production.

RateShield creates one active bucket per client, route, and policy combination. Bucket keys expire automatically, but a high number of unique clients can still create significant Redis memory pressure during the configured idle timeout window.

Recommended memory posture:

- Choose a Redis plan with an explicit memory limit.
- Estimate active buckets before production rollout.
- Monitor Redis memory usage, key count, and eviction metrics.
- Alert before memory exhaustion, usually around 70-80% usage.
- Keep `BucketIdleTimeoutSeconds` low enough to remove inactive clients.
- Revisit memory sizing after adding new protected routes or policies.

Do not treat Redis memory as unlimited. If the Redis instance runs out of memory, limiter behavior depends on the provider eviction policy and can become unsafe or noisy.

## Redis Eviction Policy

For strict production rate limiting, prefer `noeviction` when the Redis provider supports it.

With `noeviction`, Redis refuses writes when memory is full instead of silently deleting existing buckets. In RateShield, failed Redis writes flow through the configured storage failure behavior. With `FailClosed`, this protects the backend by rejecting requests when the limiter cannot safely update distributed state.

Avoid eviction policies that can remove active rate-limit buckets unexpectedly unless the tradeoff is intentional.

Risky eviction behaviors include:

- deleting active buckets under memory pressure
- resetting client quotas earlier than expected
- allowing a client to regain tokens because its bucket was evicted
- making distributed rate-limit behavior harder to reason about during traffic spikes

If a managed Redis provider does not allow configuring eviction policy, document the provider default and load test memory pressure before production use.

## Redis Connection Pooling Behavior

RateShield uses StackExchange.Redis through `IConnectionMultiplexer`.

`ConnectionMultiplexer` is designed to be shared and reused. RateShield registers it as a singleton so the gateway does not create a new Redis connection per request.

Expected behavior:

- One shared multiplexer per gateway process.
- Redis commands reuse the shared connection infrastructure.
- The token-bucket evaluator should not create or dispose Redis connections during request handling.
- Connection creation happens through dependency injection during service setup.

This keeps request processing fast and avoids exhausting Redis connection limits during traffic bursts.

If Redis connectivity becomes unstable, prefer tuning timeout values and provider/network settings before increasing gateway instance count. More gateway instances means more Redis clients and more pressure on the Redis service.

## Redis Timeout Values

RateShield exposes Redis timeout settings through configuration:

```text
RateShield:Redis:ConnectTimeoutMilliseconds
RateShield:Redis:CommandTimeoutMilliseconds
```

`ConnectTimeoutMilliseconds` controls how long the gateway waits while establishing the Redis connection.

`CommandTimeoutMilliseconds` controls how long Redis commands, including the token-bucket Lua script, can run before timing out.

Recommended starting values:

```text
ConnectTimeoutMilliseconds=5000
CommandTimeoutMilliseconds=1000
```

Production tuning guidance:

- Keep command timeouts short enough that Redis latency does not become gateway latency.
- Avoid extremely low command timeouts that cause false failures during normal provider latency.
- Increase timeouts only after checking Redis latency, CPU, memory, and network conditions.
- Revisit timeout values after moving Redis regions, providers, or plans.
- Test timeout behavior with `FailClosed` and `FailOpen` before production rollout.

Timeouts should be treated as part of the rate-limiter safety posture. A slow Redis dependency should fail predictably instead of making every API request wait too long.

## Redis Retry Behavior

RateShield should not retry token-bucket updates inside the request path by default.

The Redis Lua script is atomic, but retrying a failed or timed-out limiter operation can create uncertainty about whether the first attempt changed Redis state. A retry may also add latency to a request that is already on a failure path.

Recommended retry posture:

- Do not add automatic per-request Redis retries for limiter evaluation.
- Let Redis failures flow into `RateShield:Storage:FailureBehavior`.
- Use `FailClosed` for protected production APIs.
- Use provider-level reliability features such as managed failover where available.
- Monitor Redis timeout and connection errors instead of hiding them with repeated retries.

If retries are added later, they must be carefully bounded and tested for duplicate token consumption, latency impact, and behavior during Redis failover.

## Redis TLS Requirements

Use TLS for Redis connections whenever the Redis provider supports or requires it, especially outside private networks.

Render or another managed provider may provide different connection strings for internal/private traffic and external/public traffic. Prefer the private/internal connection string when the gateway and Redis are deployed in the same provider network.

Production TLS guidance:

- Use provider-recommended TLS connection strings.
- Keep Redis traffic on private networking where possible.
- Do not disable certificate validation in production.
- Store Redis credentials in platform secrets or environment variables.
- Avoid logging Redis connection strings because they may contain passwords.
- Verify TLS requirements before switching between local, staging, and production Redis.

Local Redis containers commonly run without TLS. That is acceptable for local development, but production configuration should match the chosen provider security model.

## Redis Authentication Requirements

Production Redis must require authentication unless it is provided through a fully managed private service that handles access control through platform networking and secrets.

RateShield receives Redis credentials through the Redis connection string:

```text
RateShield:Redis:ConnectionString
```

Authentication guidance:

- Store Redis credentials in environment variables or hosting-provider secrets.
- Do not commit Redis passwords to source control.
- Do not print Redis connection strings in logs.
- Rotate Redis credentials if they are exposed.
- Prefer provider-generated credentials over shared manual passwords.
- Use separate Redis credentials per environment when the provider supports it.

For local development, unauthenticated Redis on `localhost:6379` is acceptable. For shared development, staging, and production environments, require authentication.

## Redis Infrastructure Setup Summary

A production Redis setup for RateShield should be treated as part of the gateway infrastructure, not as an implementation detail hidden inside application code.

Minimum infrastructure decisions:

- Redis provider or deployment model.
- Redis region and network placement.
- Redis connection string secret location.
- TLS requirement.
- Authentication requirement.
- Memory limit and scaling plan.
- Eviction policy.
- Persistence posture.
- Monitoring and alerting.
- Failure behavior in RateShield.

For this project, the selected production target is Render, so the preferred Redis deployment is Render Key Value in the same region as the RateShield gateway service.

Self-hosted Redis should be limited to local development, demos, or environments where the team explicitly owns Redis operations.

## Example Redis Environment Variables

Local Redis mode:

```text
RateShield__Storage__Mode=Redis
RateShield__Storage__FailureBehavior=FailClosed
RateShield__Redis__ConnectionString=localhost:6379
RateShield__Redis__ConnectTimeoutMilliseconds=5000
RateShield__Redis__CommandTimeoutMilliseconds=1000
```

Render managed Redis mode:

```text
RateShield__Storage__Mode=Redis
RateShield__Storage__FailureBehavior=FailClosed
RateShield__Redis__ConnectionString=<render-key-value-internal-connection-string>
RateShield__Redis__ConnectTimeoutMilliseconds=5000
RateShield__Redis__CommandTimeoutMilliseconds=1000
```

Development fail-open mode:

```text
RateShield__Storage__Mode=Redis
RateShield__Storage__FailureBehavior=FailOpen
RateShield__Redis__ConnectionString=localhost:6379
RateShield__Redis__ConnectTimeoutMilliseconds=5000
RateShield__Redis__CommandTimeoutMilliseconds=1000
```

Use `FailOpen` only for development or controlled emergency recovery. Production protected routes should normally use `FailClosed`.
