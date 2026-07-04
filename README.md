# RateShield

RateShield is an ASP.NET Core reverse-proxy gateway with a token-bucket rate limiter in front of YARP.

## Documentation

- [Gateway flow](docs/gateway-flow.md)
- [Trusted proxies](docs/trusted-proxies.md): explains when RateShield can safely trust `X-Forwarded-For`.

## Configuration

RateShield uses ASP.NET Core configuration layering:

```text
appsettings.json
appsettings.{Environment}.json
environment variables
command-line args
```

`appsettings.json` contains the baseline configuration. Environment-specific files such as `appsettings.Development.json` can override local values, and environment variables can override both JSON files in hosted environments.

Use `.env.example` as the reference for environment variable names.

Nested configuration keys use double underscores.

Example:

```text
RateShield__Policies__Default__Capacity=100
```

maps to:

```json
{
  "RateShield": {
    "Policies": {
      "Default": {
        "Capacity": 100
      }
    }
  }
}
```

This keeps local, CI, staging, and production configuration aligned without hardcoding environment-specific values in code.