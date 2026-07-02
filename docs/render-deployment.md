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
RateShield__Storage__Mode=InMemory
RateShield__Storage__FailureBehavior=FailClosed
RateShield__Identity__TrustForwardedHeaders=true
RateShield__Routes__sample-api__PolicyName=Default
ReverseProxy__Clusters__sample-backend__Destinations__sample-backend-primary__Address=<hosted backend URL>
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

## Smoke Test Checklist

After deployment:

```text
GET /health/ready should return 200
GET /api/hello should proxy to the hosted backend
Repeated requests should eventually return 429 when policy limits are exceeded
Rate-limit headers should be present
Render logs should not show startup configuration errors
```

## Current V1 Limitation

V1 uses in-memory rate-limit storage. This protects a single gateway instance.

Multiple gateway instances require Redis-backed distributed storage.