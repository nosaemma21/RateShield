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
