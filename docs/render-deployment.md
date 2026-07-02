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