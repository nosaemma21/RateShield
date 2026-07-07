# Render Deployment

RateShield deploys to Render as a Docker-backed web service.

## Deployment Flow

```text
GitHub push
-> GitHub Actions
-> restore, build, test
-> Docker image build
-> publish image to GHCR
-> Render pulls the configured image
-> Render starts the gateway
```

## Deployment Steps

1. Push changes to GitHub.
2. Wait for GitHub Actions to restore, build, test, and publish the Docker image to GHCR.
3. Open the Render dashboard.
4. Create or update the Blueprint from `render.yaml`.
5. For `rateshield-gateway-staging`, provide the staging Render Key Value internal connection string for `RateShield__Redis__ConnectionString`.
6. Provide the staging backend URL for `ReverseProxy__Clusters__sample-backend__Destinations__sample-backend-primary__Address`.
7. Deploy staging and verify `/health/ready`.
8. Run smoke tests against staging.
9. For `rateshield-gateway-production`, provide the production Render Key Value internal connection string.
10. Provide the production backend URL.
11. Manually deploy production only after staging passes.
12. Verify production health, proxy forwarding, rate-limit headers, and `429` behavior.

## Required Render Settings

- Service type: Web Service
- Runtime: Docker image
- Health check path: `/health/ready`
- Internal port: `8080`
- Environment: `Production`

## Required Environment Variables

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080
RateShield__Storage__Mode=Redis
RateShield__Storage__FailureBehavior=FailClosed
RateShield__Redis__ConnectionString=<render-key-value-internal-connection-string>
RateShield__Redis__ConnectTimeoutMilliseconds=5000
RateShield__Redis__CommandTimeoutMilliseconds=1000
RateShield__Identity__TrustForwardedHeaders=true
RateShield__Routes__sample-api__PolicyName=Default
ReverseProxy__Clusters__sample-backend__Destinations__sample-backend-primary__Address=<hosted backend URL>
```

For Redis-backed distributed rate limiting, use:

```text
RateShield__Storage__Mode=Redis
RateShield__Storage__FailureBehavior=FailClosed
RateShield__Redis__ConnectionString=<render-key-value-internal-connection-string>
RateShield__Redis__ConnectTimeoutMilliseconds=5000
RateShield__Redis__CommandTimeoutMilliseconds=1000
```

## GHCR Image

Render pulls the gateway image from GitHub Container Registry:

```text
ghcr.io/nosaemma21/rateshield-gateway:latest
```

For production releases, prefer immutable commit SHA tags instead of `latest`.

## Backend Destination

The YARP backend destination must point to a hosted backend URL.

Do not use:

```text
http://localhost:5255/
```

inside Render. In a container, `localhost` means the gateway container itself.

## Reverse Proxy And Load Balancer Notes

Render terminates public HTTPS at the platform edge and forwards traffic to the RateShield container over the internal service connection.

RateShield should listen on HTTP inside the container:

```text
ASPNETCORE_URLS=http://0.0.0.0:8080
```

Do not require the container itself to terminate public HTTPS when running on Render.

When forwarded headers are trusted, only trust headers from the hosting platform or known upstream proxies. Do not trust arbitrary client-supplied `X-Forwarded-For` values.

RateShield uses the configured YARP cluster destination as the backend target. In hosted environments, this destination must be a real hosted backend URL, not `localhost`.

If a CDN or external load balancer is placed in front of Render, update the trusted proxy strategy and document which headers are accepted.

## HTTPS And TLS Termination

Render provides public HTTPS for the deployed web service and terminates TLS before forwarding traffic to the RateShield container.

RateShield should run HTTP inside the container:

```text
ASPNETCORE_URLS=http://0.0.0.0:8080
```

Do not bind Kestrel to a public HTTPS certificate inside the container for the Render deployment path unless the hosting architecture changes.

For production:

- use the Render-provided HTTPS URL or a configured custom domain with HTTPS enabled
- keep backend destination URLs HTTPS when the upstream backend is public
- use private/internal backend URLs only when the backend is reachable securely within the hosting platform
- do not commit certificates, private keys, or TLS secrets to the repository
- document any CDN, WAF, or external load balancer that terminates TLS before traffic reaches Render

If TLS is terminated before RateShield, make sure forwarded headers are trusted only from known infrastructure so the gateway can reason safely about the original request scheme.

## Autoscaling Notes

RateShield is designed to support multiple gateway instances when `RateShield__Storage__Mode=Redis`.

Autoscaling should only be enabled when:

- Redis mode is configured
- `/health/ready` verifies Redis connectivity
- all gateway instances use the same Redis backplane for the same environment
- backend services can handle the increased forwarded traffic
- rate-limit policies have been reviewed for production traffic patterns

Do not rely on in-memory storage for autoscaled production deployments. Each instance would keep its own local token buckets, which would allow clients to exceed the intended global limit.

When autoscaling, monitor:

- request latency
- rejected request count
- Redis command latency
- Redis memory usage
- gateway CPU and memory usage
- backend error rates

## Horizontal Scaling Notes

Horizontal scaling means running more than one RateShield gateway instance behind Render's platform routing.

In Redis mode, token-bucket state is shared through Render Key Value, so all gateway instances evaluate limits against the same distributed bucket state.

Use separate Redis/Key Value instances for staging and production. Sharing one Redis instance across environments can mix test and production traffic, which makes limits inaccurate and troubleshooting harder.

All horizontally scaled instances must use the same configuration values for:

- `RateShield__Storage__Mode`
- `RateShield__Storage__FailureBehavior`
- `RateShield__Redis__ConnectionString`
- rate-limit policies
- route-to-policy mappings
- trusted proxy settings
- backend destination URLs

If policies are changed, deploy the same image and configuration consistently across every running instance.

## Deployment Rollback Notes

Production deployments should use immutable image tags, such as a commit SHA or release tag, so rollback can return to a known working image.

To roll back a Render deployment:

1. Identify the last known good gateway image tag.
2. Update the production service image reference to that tag.
3. Redeploy the production service manually.
4. Verify `/health/ready`.
5. Run production smoke tests.
6. Confirm rate-limit headers and `429` responses still behave correctly.
7. Review Render logs for startup, Redis, or YARP errors.

Rollback should not require changing Redis data. If a rollback also needs policy changes, document the exact policy values before applying them.

Keep staging on the newer candidate image when investigating production rollback issues unless staging itself is broken.

## Render Key Value Setup

Use Render Key Value as the managed Redis-compatible backplane for distributed RateShield storage.

Recommended setup:

- Create a Render Key Value instance in the same region as the RateShield gateway service.
- Use the internal connection string when the gateway and Key Value instance are in the same Render private network.
- Store the connection string as a Render environment variable.
- Map the connection string to `RateShield__Redis__ConnectionString`.
- Set `RateShield__Storage__Mode=Redis`.
- Keep `RateShield__Storage__FailureBehavior=FailClosed` for protected production APIs.

Do not commit the Render Key Value connection string to `appsettings.json`, `.env`, `render.yaml`, Docker files, or documentation.

For staging and production, prefer separate Render Key Value instances so test traffic cannot affect production limiter state.

## Redis Readiness

When Redis mode is enabled, `/health/ready` should reflect whether required dependencies are usable.

If readiness fails after enabling Redis mode, check:

- the Render Key Value instance is running
- the connection string is present
- the connection string is mapped to `RateShield__Redis__ConnectionString`
- the gateway and Redis service are in compatible regions/networks
- Redis authentication or TLS requirements match the provider connection string

## Smoke Test Checklist

After deployment:

```text
GET /health/ready should return 200
GET /api/hello should proxy to the hosted backend
Repeated requests should eventually return 429 when policy limits are exceeded
Rate-limit headers should be present
Render logs should not show startup configuration errors
```

## Storage Mode Guidance

In-memory mode protects a single gateway instance.

Redis mode should be used when RateShield is horizontally scaled or when all gateway instances must share the same token buckets.
