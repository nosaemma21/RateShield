# Observability

RateShield includes structured logs, correlation IDs, and metrics for understanding gateway behavior in local, CI, staging, and production environments.

## Structured Logs

RateShield logs rate-limit decisions from the gateway middleware.

Allowed requests are logged at `Debug`.

Rejected requests are logged at `Warning`.

Logged fields include:

```text
RouteId
PolicyName
ClientSource
StorageMode
Remaining
Limit
RetryAfterSeconds
RejectionReason
```

RateShield logs `ClientSource`, not raw client identity values. This avoids leaking API keys, client IDs, or tokens into logs.

## Correlation IDs

RateShield uses:

```text
X-Correlation-ID
```

If the client sends this header, RateShield preserves it.

If the client does not send it, RateShield generates one.

The correlation ID is added to:

```text
request headers
response headers
logging scope
upstream forwarded requests
```

This helps connect gateway logs with backend logs for the same request.

## Metrics

RateShield emits metrics using .NET `System.Diagnostics.Metrics`.

The meter name is:

```text
RateShield
```

Custom RateShield metrics include:

```text
rateshield.requests.allowed
rateshield.requests.rejected
rateshield.buckets.active
rateshield.cleanup.runs
rateshield.cleanup.removed_buckets
rateshield.errors
```

Metric tags include:

```text
route.id
policy.name
client.source
storage.mode
error.type
```

## OpenTelemetry

OpenTelemetry is configured to collect RateShield metrics and ASP.NET Core metrics.

RateShield registers:

```text
RateShield
ASP.NET Core instrumentation
```

This allows the gateway to export metrics to supported observability backends.

## Prometheus Endpoint

RateShield exposes a Prometheus-compatible metrics endpoint:

```text
GET /metrics
```

Prometheus output is plain text and machine-readable.

Example shape:

```text
# HELP rateshield_requests_allowed Number of requests allowed by RateShield.
# TYPE rateshield_requests_allowed counter
rateshield_requests_allowed{route_id="sample-api"} 12
```

OpenTelemetry converts metric names for Prometheus format, so:

```text
rateshield.requests.allowed
```

appears as:

```text
rateshield_requests_allowed
```

## Local Testing

Run the gateway:

```powershell
dotnet run --project src/RateShield.Gateway
```

Send traffic through a protected route:

```powershell
curl -i http://localhost:5011/api/hello
curl -i http://localhost:5011/api/hello
```

Inspect metrics:

```powershell
curl http://localhost:5011/metrics
```

Look for:

```text
rateshield_requests_allowed
rateshield_requests_rejected
rateshield_buckets_active
```

## Prometheus Exporter Note

The direct ASP.NET Core Prometheus exporter uses a prerelease OpenTelemetry package in V1.

For stricter production deployments, prefer this path:

```text
RateShield
-> OpenTelemetry OTLP exporter
-> OpenTelemetry Collector
-> Prometheus scrapes collector
-> Grafana dashboards
```

The direct `/metrics` endpoint is useful for local development, demos, and simple deployments.

## What Remains

Future observability improvements:

```text
Grafana dashboard
OpenTelemetry Collector setup
hosted metrics backend selection
storage error metrics for Redis mode
distributed tracing
production alert rules
```