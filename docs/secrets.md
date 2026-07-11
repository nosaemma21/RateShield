# Secret Management

RateShield must not commit production secrets to source control.

Secrets include:

```text
Redis connection strings
API keys
client secrets
registry credentials
deployment tokens
provider access tokens
```

## Configuration Rule

Use normal configuration keys, but provide secret values through environment variables or hosting-provider secret storage.

Example configuration key:

```text
RateShield:Redis:ConnectionString
```

Environment variable equivalent:

```text
RateShield__Redis__ConnectionString
```

ASP.NET Core maps double underscores to nested configuration sections.

## Render

On Render, store secrets in the service environment variables.

For Redis mode, configure:

```text
RateShield__Storage__Mode=Redis
RateShield__Redis__ConnectionString=<Render Key Value connection string>
```

Do not place the Redis connection string in:

```text
appsettings.json
appsettings.Production.json
render.yaml
README.md
docs
```

## GitHub Actions

Use GitHub Actions secrets for CI/CD values.

Examples:

```text
GHCR_TOKEN
RENDER_API_KEY
RENDER_SERVICE_ID
```

Do not print secrets in workflow logs.

Do not echo secret values in scripts.

## CI/CD Secret Storage Strategy

RateShield separates secrets from non-secret deployment settings.

Use GitHub Actions secrets for values that grant access or trigger privileged actions:

```text
RENDER_API_KEY
RENDER_WORKSPACE_ID
RENDER_STAGING_DEPLOY_HOOK_URL
RENDER_PRODUCTION_DEPLOY_HOOK_URL
```

Use GitHub Actions variables for non-secret deployment settings:

```text
ENABLE_STAGING_DEPLOY=true
RATESHIELD_STAGING_BASE_URL=https://replace-with-staging-url.onrender.com
RATESHIELD_PRODUCTION_BASE_URL=https://replace-with-production-url.onrender.com
```

Use Render service environment variables for runtime application configuration:

```text
ASPNETCORE_ENVIRONMENT=Staging
ASPNETCORE_URLS=http://0.0.0.0:8080
RateShield__Storage__Mode=Redis
RateShield__Storage__FailureBehavior=FailClosed
RateShield__Redis__ConnectionString=<render-key-value-internal-connection-string>
ReverseProxy__Clusters__sample-backend__Destinations__sample-backend-primary__Address=<backend-url>
```

For production, use the same keys with production values:

```text
ASPNETCORE_ENVIRONMENT=Production
RateShield__Redis__ConnectionString=<production-render-key-value-internal-connection-string>
ReverseProxy__Clusters__sample-backend__Destinations__sample-backend-primary__Address=<production-backend-url>
```

Do not store deploy hook URLs, Redis connection strings, API keys, bearer tokens, or provider credentials in:

```text
appsettings.json
appsettings.Staging.json
appsettings.Production.json
render.yaml
Dockerfile
docker-compose.yml
GitHub Actions YAML
documentation examples
```

Committed files may contain placeholders only.

## Local Development

For local development, use one of:

```text
.NET user secrets
local .env file that is ignored by git
PowerShell environment variables
Docker Compose override files ignored by git
```

Never commit local secret files.

## Safe Examples

Docs and examples should show placeholders only:

```text
RateShield__Redis__ConnectionString=<redis-connection-string>
```

Do not use real hostnames, passwords, tokens, or usernames in public examples.

## Rotation

If a secret is accidentally committed:

```text
revoke it immediately
rotate the credential
remove it from history if necessary
audit logs for usage
```

Treat committed secrets as compromised.
