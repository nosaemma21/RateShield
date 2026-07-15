# Load And Performance Results

This document records the local k6 results used to validate RateShield's load
behavior. These measurements are development evidence, not production service
level objectives or capacity guarantees.

## Test Environment

The tests ran locally on Windows with Docker Desktop. Docker Compose hosted the
gateway, sample backend, and Redis. Tests that specifically measured in-memory
bucket behavior used a separate gateway container configured with
`RateShield__Storage__Mode=InMemory`.

The host hardware profile and background system load were not captured. Results
therefore describe this test run only and should be repeated in the target
hosting region with production-equivalent gateway and Redis resources.

## Results

| Test | Workload | Result |
| --- | --- | --- |
| Smoke | 5 requests/second for 10 seconds | All 51 checks passed with 0% HTTP failures; p95 was 39.58 ms. |
| Single-client burst | 200 requests shared across 20 virtual users | 100 requests were allowed and 100 received the expected HTTP 429 response with `Retry-After`. |
| Sustained single client | 10 requests/second for 30 seconds | No HTTP failures or dropped iterations; p95 was 12.84 ms. |
| Many unique clients | 1,000 iterations across 50 virtual users | All requests completed with no unexpected failures; p95 was 73.12 ms. |
| Mixed policies | 50 requests to the Default route and 50 to the Strict route | The Default route, with capacity 100, rejected none. The Strict route, with capacity 20, rejected 30 requests. |
| Direct-versus-gateway latency | 10 requests/second to both paths for 30 seconds | Direct backend p95 was 7.22 ms and gateway p95 was 13.06 ms. Measured gateway p95 overhead was 5.84 ms. |
| In-memory bucket growth | 10,000 additional unique-client buckets | Container memory grew from 44.47 MiB to 192.7 MiB: 148.23 MiB total, approximately 15.18 KiB per bucket. |
| Cleanup under load | 10,000 seeded buckets plus 100 requests/second for 15 seconds | All 11,500 requests passed with no failures or dropped iterations. Traffic p95 was 96.18 ms. Cleanup removed 10,051 buckets and the active count eventually reached zero. |
| Two Redis-backed gateways | 200 alternating requests for one client, split evenly across two instances | Each gateway received 100 requests. Redis enforced one shared bucket: 100 requests were allowed and 100 were rate limited. Request p95 was 104.25 ms. |

## Gateway Latency Overhead

The direct-backend and gateway paths were exercised concurrently at the same
arrival rate. The recorded distributions were:

| Measurement | Average | Median | p95 |
| --- | ---: | ---: | ---: |
| Direct backend | 3.26 ms | 2.27 ms | 7.22 ms |
| Through RateShield | 5.65 ms | 4.04 ms | 13.06 ms |
| Measured overhead | 2.39 ms | 1.77 ms | 5.84 ms |

The overhead is the difference between the two locally observed paths. It
includes gateway processing and local Docker networking, so it is not a pure
measurement of token-bucket execution time.

## Memory And Cleanup Interpretation

The measured 15.18 KiB per in-memory bucket is derived from container-level
memory growth. It includes runtime allocations, dictionary overhead, and other
process activity; it is not the serialized size of one `TokenBucketState`.

During the cleanup test, traffic p95 increased from the earlier sustained-test
baseline of 12.84 ms to 96.18 ms while bucket seeding and repeated cleanup scans
were active. Reliability remained intact: there were no unexpected HTTP
failures or dropped k6 iterations. Production sizing should still test larger
bucket counts and the intended `MaxBucketsPerScan` value on representative
hardware.

## Distributed Redis Result

Two gateway containers connected to the same Redis instance and alternated
requests for one client identity. The combined result was exactly one policy
capacity: 100 allowed requests followed by 100 expected rate-limit responses.
This verifies that horizontal gateway instances do not receive independent
allowances when Redis mode is enabled.

## Reproduce The Standard Tests

Start the Compose environment and confirm the gateway is ready:

```powershell
docker compose up --detach --build
(Invoke-WebRequest -UseBasicParsing http://localhost:8080/health/ready).StatusCode
```

Run the standard scripts from the repository root:

```powershell
k6 run tests/load/rateshield-smoke.js
k6 run tests/load/rateshield-burst.js
k6 run tests/load/rateshield-sustained.js
k6 run tests/load/rateshield-many-clients.js
k6 run tests/load/rateshield-mixed-routes.js
k6 run tests/load/rateshield-latency.js
```

The memory, cleanup, and multi-instance scripts require their dedicated
container arrangements because they use port 8082, in-memory cleanup overrides,
or two Redis-backed gateways. The scripts are:

```text
tests/load/rateshield-memory.js
tests/load/rateshield-cleanup.js
tests/load/rateshield-multi-instance.js
```

Use a new client or run identifier when repeating a Redis test so an existing
bucket cannot affect the result.

## Benchmark Limitations

- Results came from one local development machine and one run of each workload.
- The tests did not establish maximum throughput or saturation limits.
- Local Docker networking does not reproduce hosted network latency.
- The Redis instance was local and does not represent a managed cross-network
  Redis deployment.
- Longer soak tests are still required to assess sustained memory behavior,
  garbage collection, connection stability, and tail latency.
- Production acceptance thresholds should be defined from product requirements
  and verified in the target deployment environment.
