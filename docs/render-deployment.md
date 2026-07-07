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
