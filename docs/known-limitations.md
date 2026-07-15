# Known Limitations

RateShield is a production-foundation project, not a complete API-management
platform. This document records the current boundaries so operators can make
safe deployment decisions and contributors can distinguish intentional scope
from unfinished work.

## Rate-Limiting Model

### Token Bucket Is The Only Algorithm

RateShield implements token-bucket rate limiting. It does not currently offer
fixed-window, sliding-window, concurrency, bandwidth, or quota-based policies.

Use route-specific token-bucket policies where they fit. A requirement for a
different algorithm needs a new evaluator and configuration contract rather
than an approximation hidden inside the existing policy.

### Policy Cost Is Static

`RequestCost` is configured per policy. It cannot currently vary by request
body size, response status, authenticated subscription tier, or another runtime
attribute.

Create separate routes and policies when operations need different fixed costs.

### Configuration Changes Require A Restart

RateShield binds and validates its options during startup. It does not provide a
management API, configuration database, or guaranteed hot reload for policies,
route mappings, identity rules, or storage settings.

Treat configuration as deployment configuration and restart or redeploy the
gateway after changing it.

## Client Identity

### Identity Headers Are Identifiers, Not Authentication

RateShield can use API-key and client-ID headers as bucket identities, but it
does not authenticate those values or verify that a caller owns them. A client
that can freely choose the configured header can rotate values and obtain fresh
buckets.

Authenticate callers before RateShield, or ensure a trusted proxy removes
client-supplied identity headers and injects verified values. Bearer-claim
identity also depends on an authentication component having already populated
the ASP.NET Core user principal.

### Trusted Proxy Matching Uses Exact IP Addresses

Forwarded client IPs are accepted only when the immediate peer matches an entry
in `TrustedProxyIpAddresses`. The current implementation does not accept CIDR
ranges, resolve proxy hostnames, or automatically track cloud-provider proxy
address changes.

Keep the exact proxy IP list current. Do not enable forwarded-header trust when
the expected immediate proxy addresses are unknown or unstable.

### Forwarded Chains Use The Leftmost Address

When the immediate peer is trusted, RateShield uses the first address in the
configured forwarded-for header. It does not walk a multi-proxy chain from
right to left or maintain a separate trusted-network model for every hop.

Ensure the trusted edge normalizes the header and prevents callers from
injecting an arbitrary leftmost value.

## Storage And Scaling

### In-Memory Limits Are Per Instance

Each in-memory gateway has independent buckets. Horizontal replicas therefore
multiply the effective client allowance and lose state when a process restarts.

Use Redis mode whenever more than one gateway instance must enforce one shared
quota.

### In-Memory Cleanup Is A Bounded Scan

Cleanup enumerates up to `MaxBucketsPerScan` idle candidates at each interval.
Large or adversarial identity cardinality can grow memory faster than cleanup
removes buckets. Local testing also showed higher tail latency while large
bucket sets were created and removed.

Tune the idle timeout, scan limit, and interval against representative traffic;
monitor active buckets and process memory; and use Redis for horizontally
scaled or high-cardinality deployments.

### Redis Is A Request-Path Dependency

Redis mode performs the atomic token-bucket decision through Redis for each
protected request. Redis network latency therefore contributes directly to
gateway latency. RateShield does not retry limiter updates in the request path
or automatically fall back to local buckets.

Choose `FailClosed` or `FailOpen` deliberately, deploy Redis close to the
gateway, and provide Redis availability, memory, security, persistence, and
monitoring appropriate to the workload.

### Redis State Is Not Business Data

If Redis loses rate-limit keys, affected clients receive fresh buckets and can
temporarily regain capacity. Redis persistence reduces this risk but does not
turn limiter state into an abuse-prevention audit record.

Use additional authentication, authorization, fraud controls, or durable quota
tracking when those guarantees are required.

## Proxy And Availability Behavior

### Readiness Does Not Check The Upstream Backend

In Redis mode, `/health/ready` checks Redis. It does not currently verify that
the configured YARP backend is reachable, so readiness can remain healthy while
the protected backend is unavailable.

Monitor the backend separately and use YARP destination health behavior or
platform-specific probes appropriate to the deployed service topology.

### Active Destination Health Checks Are Not Configured

The supplied YARP configuration uses passive destination health checks. It does
not assume that every backend exposes a common active probe endpoint.

Add an active health-check path when all destinations behind a cluster expose a
stable endpoint with suitable semantics.

### No Default Request Or Response Transforms

The sample routes preserve the incoming path and normal proxy headers. RateShield
does not ship a universal transform policy for rewriting paths, hosts, or
application-specific headers.

Configure YARP transforms per route or cluster when an upstream contract
requires them.

## Protocol And Response Surface

### Rate-Limit Headers Use The Existing `X-RateLimit-*` Contract

RateShield returns `X-RateLimit-Limit`, `X-RateLimit-Remaining`, and
`X-RateLimit-Reset`, plus `Retry-After` on rejection. It does not currently emit
the newer combined `RateLimit` and `RateLimit-Policy` fields while their IETF
specification remains unfinished.

Treat the current headers as RateShield's public compatibility contract and
review the decision when the relevant specification becomes an RFC.

### HTTP And TLS Termination Depend On The Host

The container listens on HTTP internally. The provided Render design expects
the platform edge to terminate public HTTPS. RateShield does not manage public
certificates inside the container.

Use a trusted ingress, load balancer, or hosting edge for TLS and configure
forwarded headers only for that trusted hop.

## Observability

### Metrics Need External Collection And Alerting

RateShield exposes process-local OpenTelemetry metrics through Prometheus or the
console exporter. It does not include a Prometheus server, long-term metrics
storage, dashboards, paging rules, or a distributed tracing backend.

Scrape every gateway replica and aggregate externally. Define alerts for
rejections, storage errors, latency, Redis health, bucket growth, and process
memory.

### The Metrics Endpoint Has No Built-In Authentication

When the Prometheus exporter is enabled, the scraping endpoint is mapped by the
gateway. RateShield does not add authentication to that endpoint.

Restrict metrics access through private networking, ingress rules, or an
authenticated observability proxy in hosted environments.

## Validation Status

The local Docker, automated test, load-test, cleanup, and two-instance Redis
paths have been exercised. The following still require environment-specific
validation before calling a deployment production-ready:

- A real hosted staging deployment and rollback.
- Backend-unavailable behavior with the deployed proxy and platform.
- End-to-end pull-request, image publication, staging, and tagged production
  release execution using real repository secrets.
- Long-duration soak, saturation, and failure-injection tests on
  production-equivalent resources.
- Confirmation that hosted proxy IPs, TLS, Redis networking, metrics access,
  and alerting match the documentation.

See [Load and performance results](performance.md) for the measured local
workloads and their benchmarking limitations.
