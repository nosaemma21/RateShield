# Redis Distributed Storage Design

Redis mode is the V2 distributed storage path for RateShield.

The goal is to allow multiple RateShield gateway instances to enforce the same rate-limit policy for the same client and route.

## Why Redis Is Needed

The in-memory limiter works only inside one gateway process.

If there are multiple gateway instances, each instance has its own local buckets.

That means a client could exceed the intended limit by spreading requests across instances.

Redis gives RateShield a shared backplane.

```text
Client
-> Load balancer
-> RateShield instance A
-> Redis bucket state

Client
-> Load balancer
-> RateShield instance B
-> same Redis bucket state
```

## Redis Key Format

Redis keys should uniquely identify:

```text
RateShield application namespace
route ID
policy name
client identity
```

Recommended key format:

```text
rateshield:v1:bucket:{routeId}:{policyName}:{clientHash}
```

Example:

```text
rateshield:v1:bucket:sample-api:Default:8f14e45fceea167a5a36dedd4bea2543
```

## Client Identity Hashing

Raw client identity values should not be placed in Redis keys.

Do not store raw values like:

```text
API keys
bearer tokens
client secrets
full user identifiers
```

Instead, hash the client identity before building the Redis key.

Reason:

```text
Redis keys may appear in logs
Redis keys may be visible to operators
Redis keys may be exported during diagnostics
```

## Bucket Value Format

A Redis bucket needs to store:

```text
available tokens
last refill timestamp
last seen timestamp
```

Recommended Redis hash fields:

```text
tokens
last_refilled_at_unix_ms
last_seen_at_unix_ms
```

Example:

```text
HSET rateshield:v1:bucket:sample-api:Default:<clientHash>
  tokens 42
  last_refilled_at_unix_ms 1783000000000
  last_seen_at_unix_ms 1783000000500
```

## TTL Strategy

Each Redis bucket should expire after it has been idle long enough.

The TTL should be based on the configured bucket idle timeout.

Example:

```text
EXPIRE bucketKey bucketIdleTimeoutSeconds
```

Every successful bucket update should refresh the TTL.

This prevents Redis memory from growing forever for clients that disappear.

## Atomic Update Strategy

Redis updates must be atomic.

Do not implement the distributed limiter as separate commands like:

```text
GET bucket
calculate locally
SET bucket
```

That creates race conditions when multiple gateway instances update the same bucket at the same time.

Use a Lua script so Redis performs the full token-bucket calculation atomically.

## Lua Script Inputs

The Lua script should receive:

```text
bucket key
requested timestamp
capacity
refill tokens
refill period milliseconds
request cost
bucket idle timeout seconds
```

## Lua Script Outputs

The Lua script should return:

```text
allowed flag
remaining tokens
retry-after milliseconds
reset-at unix milliseconds
```

This gives the gateway everything it needs to build the HTTP response.

## Redis Time Source

Preferred V2 approach:

```text
Use Redis TIME inside the Lua script.
```

Reason:

```text
all gateway instances share Redis time
clock skew between gateway instances does not affect refill behavior
```

If Redis TIME is not used, gateway clocks must be tightly synchronized.

## Clock Skew

When Redis is the source of time, gateway instance clock skew is mostly irrelevant for rate-limit math.

The gateway can still use local time for logs, but limiter calculations should use Redis time.

## Redis Timeout Behavior

Redis operations must have a short timeout.

RateShield should not let Redis slowness stall request processing indefinitely.

Timeout behavior must follow configured storage failure behavior.

## Failure Behavior

Production default:

```text
FailClosed
```

Meaning:

```text
if Redis is unavailable, protected traffic is rejected
```

This protects backend services during limiter uncertainty.

Development-only option:

```text
FailOpen
```

Meaning:

```text
if Redis is unavailable, traffic is allowed
```

Fail-open is convenient for local development but risky in production.

Optional future mode:

```text
LocalFallback
```

Meaning:

```text
if Redis is unavailable, temporarily use in-memory buckets
```

This improves availability but weakens global consistency.

## Local vs Redis Mode

In-memory mode:

```text
fast
simple
no external dependency
not distributed
not safe for multiple gateway instances
```

Redis mode:

```text
distributed
consistent across gateway instances
requires Redis availability
adds network latency
requires secret management
```

## Memory Sizing

Redis memory use is roughly proportional to:

```text
number of active clients
number of protected routes
number of active policies
bucket idle timeout duration
```

Longer TTLs keep buckets around longer and use more memory.

## Eviction Policy

Production Redis should not evict active limiter keys unexpectedly.

Prefer a Redis plan with enough memory for expected active bucket count.

Avoid eviction policies that randomly remove limiter state unless the operational tradeoff is understood.

## V2 Acceptance Criteria

Redis mode is ready when:

```text
token bucket updates are atomic
multiple gateway instances share bucket state
Redis keys do not expose raw client identities
bucket TTL prevents unbounded memory growth
Redis timeout behavior is explicit
storage failure behavior is configurable
integration tests prove distributed behavior
```
