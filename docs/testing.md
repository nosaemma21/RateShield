# Testing Guide

RateShield uses xUnit v3 for automated .NET tests and k6 for load and
performance tests. The default solution test run covers the token-bucket core,
gateway middleware and configuration, proxy integration, health endpoints, and
the Redis Lua implementation.

## Prerequisites

Install or start the dependency required by the test type you plan to run:

| Test type | Requirement |
| --- | --- |
| Core and most gateway tests | .NET 8 SDK |
| Redis integration test | Docker Desktop or another reachable Docker engine |
| Load and performance tests | k6 plus the appropriate local gateway containers |

Verify the local tools:

```powershell
dotnet --version
docker version
k6 version
```

Docker is not required for the core test project. It is required when the
gateway test project starts its temporary `redis:7-alpine` container through
Testcontainers.

## Run The Complete .NET Suite

From the repository root:

```powershell
dotnet test RateShield.sln
```

This restores, builds, and runs both test projects:

```text
tests/RateShield.Core.Tests
tests/RateShield.Gateway.Tests
```

Docker Desktop must be running for the complete suite because the gateway tests
exercise the real Redis Lua script against a disposable Redis container.

## Fast Local Test Loops

Run only the core project when changing token-bucket calculations, cleanup, or
policy resolution:

```powershell
dotnet test tests/RateShield.Core.Tests/RateShield.Core.Tests.csproj
```

Run the gateway project when changing middleware, identity, validation,
observability integration, proxy forwarding, health checks, or Redis behavior:

```powershell
dotnet test tests/RateShield.Gateway.Tests/RateShield.Gateway.Tests.csproj
```

The core suite uses controllable test doubles such as `FakeClock`, so limiter
and cleanup behavior can be tested deterministically without waiting for real
time to pass.

## What The Test Projects Cover

### Core Tests

- Initial token capacity and token consumption.
- Refill calculations, elapsed refill periods, and capacity caps.
- Requests whose cost exceeds the available tokens.
- Retry timing after rejection.
- Policy lookup and missing-policy behavior.
- Same-client concurrency and many-client bucket creation.
- Idle bucket detection, scan limits, and cleanup removal.

### Gateway Tests

- Rate-limiting middleware allow, reject, bypass, and failure behavior.
- Client identity selection and forwarded-header handling.
- Correlation IDs and sensitive-header redaction.
- Startup validation for policies, routes, cleanup, identity, rejection
  responses, and Redis configuration.
- YARP forwarding for common HTTP methods, bodies, queries, and headers.
- Liveness and readiness behavior.
- Redis key construction, result mapping, and failure behavior.
- Atomic Redis token consumption and rejection against a real temporary Redis
  container.

## Redis Integration Tests

`RedisTokenBucketScriptExecutorIntegrationTests` starts a temporary
`redis:7-alpine` container, connects through StackExchange.Redis, executes the
same Lua script used by the gateway, and disposes the container after the test
class completes.

You do not need to start the repository's Compose Redis service for this test.
You only need a working Docker engine:

```powershell
docker info
dotnet test tests/RateShield.Gateway.Tests/RateShield.Gateway.Tests.csproj
```

If Docker is stopped or inaccessible, the Testcontainers test will fail while
starting Redis. Start Docker Desktop and rerun the gateway test project.

## Match The CI Validation Locally

GitHub Actions runs the following sequence for pull requests and pushes to
`main`:

```powershell
dotnet restore RateShield.sln
dotnet tool restore
dotnet format RateShield.sln --verify-no-changes --verbosity minimal
dotnet list RateShield.sln package --vulnerable --include-transitive
dotnet build RateShield.sln --configuration Release --no-restore -warnaserror
dotnet dotnet-coverage collect "dotnet test RateShield.sln --configuration Release --no-build --results-directory TestResults" -f cobertura -o TestResults/coverage.cobertura.xml
```

This sequence checks formatting, audits direct and transitive packages, treats
build warnings as errors, runs the Release build tests, and writes Cobertura
coverage to:

```text
TestResults/coverage.cobertura.xml
```

CI uploads the `TestResults` directory and coverage file as workflow artifacts,
even when a later step fails.

## Load And Performance Tests

The k6 scripts live in `tests/load`:

| Script | Purpose |
| --- | --- |
| `rateshield-smoke.js` | Low-rate gateway health check. |
| `rateshield-burst.js` | Same-client burst and expected HTTP 429 behavior. |
| `rateshield-sustained.js` | Stable single-client traffic and dropped-iteration detection. |
| `rateshield-many-clients.js` | High-cardinality client bucket creation. |
| `rateshield-mixed-routes.js` | Independent Default and Strict route policies. |
| `rateshield-latency.js` | Direct-backend versus gateway latency. |
| `rateshield-memory.js` | In-memory growth from many unique buckets. |
| `rateshield-cleanup.js` | Traffic latency and reliability while idle buckets are removed. |
| `rateshield-multi-instance.js` | Shared Redis enforcement across two gateways. |

For the standard Compose-backed scripts, start the environment first:

```powershell
docker compose up --detach --build
(Invoke-WebRequest -UseBasicParsing http://localhost:8080/health/ready).StatusCode
```

Then run the desired script, for example:

```powershell
k6 run tests/load/rateshield-smoke.js
k6 run tests/load/rateshield-burst.js
k6 run tests/load/rateshield-sustained.js
k6 run tests/load/rateshield-many-clients.js
k6 run tests/load/rateshield-mixed-routes.js
k6 run tests/load/rateshield-latency.js
```

Many scripts accept environment-variable overrides through k6's `-e` option:

```powershell
k6 run -e BASE_URL=http://localhost:8082 tests/load/rateshield-smoke.js
k6 run -e RATE=20 -e DURATION=60s tests/load/rateshield-sustained.js
```

The memory, cleanup, and multi-instance tests need the dedicated container
arrangements described in the load test workflow. Their recorded setup,
measurements, and limitations are documented in
[Load and performance results](performance.md).

## Reading k6 Results

- `checks` reports whether the functional assertions passed.
- `http_req_failed` reports responses k6 considered unexpected failures.
- `http_req_duration` reports end-to-end request latency.
- `p(95)` is the latency at or below which 95% of measured requests completed.
- `dropped_iterations` means k6 could not start scheduled work in time; it does
  not mean the gateway returned an HTTP error.
- An HTTP 429 is an expected result in rate-limit tests when the script includes
  it in its assertions or expected-status callback.

A test is complete only when its functional checks and relevant thresholds pass.
Do not judge a run from request count alone.

## Troubleshooting

### Redis Test Cannot Start

Confirm Docker Desktop is running and accessible:

```powershell
docker info
```

The integration test manages its own Redis container. Do not hardcode a local
Redis port into the test.

### k6 Reports Connection Refused

Check the URL printed in the warning. Compose exposes the normal gateway on
port 8080; dedicated load-test containers commonly use ports 8082 and 8083.
Pass the correct script environment variable when the default does not match.

### A Redis Load Test Gives Different Counts

Use a new client ID or run ID so a bucket from an earlier run cannot affect the
starting capacity:

```powershell
$clientId = "load-test-$([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds())"
k6 run -e CLIENT_ID=$clientId tests/load/rateshield-multi-instance.js
```

### Formatting Passes Locally But Fails In CI

Run the same repository-level command used by CI:

```powershell
dotnet format RateShield.sln --verify-no-changes --verbosity minimal
```

Check line endings and ensure generated or unrelated files were not staged.

## Adding Tests

- Keep Arrange, Act, and Assert sections clear.
- Prefer deterministic clocks and in-memory configuration over real delays.
- Give Redis test keys and load-test client identities unique, descriptive names.
- Test observable behavior rather than private implementation details.
- Cover both the successful path and the relevant rejection or failure path.
- Run the smallest affected test project while developing, then run the full
  solution before opening a pull request.
