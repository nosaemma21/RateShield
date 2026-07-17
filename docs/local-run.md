# Local Run Guide

RateShield runs locally with two processes:

```text
RateShield.SampleBackend -> http://localhost:5255
RateShield.Gateway       -> http://localhost:5011
```

The gateway receives client traffic and forwards matching routes to the sample backend through YARP.

## Run The Sample Backend

From the repository root:

```powershell
dotnet run --project src/RateShield.SampleBackend
```

Direct backend checks:

```powershell
curl.exe -i http://localhost:5255/
curl.exe -i "http://localhost:5255/api/hello?source=backend"
curl.exe -i http://localhost:5255/slow
curl.exe -i http://localhost:5255/fail
curl.exe -i http://localhost:5255/headers
curl.exe -i -X POST http://localhost:5255/echo -H "Content-Type: application/json" -d "{\"message\":\"hello\"}"
```

## Run The Gateway

In a second terminal, from the repository root:

```powershell
dotnet run --project src/RateShield.Gateway
```

Gateway checks:

```powershell
curl.exe -i http://localhost:5011/health/live
curl.exe -i http://localhost:5011/health/ready
curl.exe -i "http://localhost:5011/api/hello?source=gateway"
```

## Test Rate Limiting

Use a strict temporary policy while testing repeated requests:

```json
"Default": {
  "Capacity": 1,
  "RefillTokens": 1,
  "RefillPeriodSeconds": 300,
  "RequestCost": 1
}
```

Then call the same protected route twice:

```powershell
curl.exe -i http://localhost:5011/api/hello
curl.exe -i http://localhost:5011/api/hello
```

Expected result:

```text
First request  -> allowed
Second request -> HTTP 429 Too Many Requests
```

Allowed and rejected responses should include rate-limit headers:

```text
X-RateLimit-Limit
X-RateLimit-Remaining
X-RateLimit-Reset
Retry-After
```

`Retry-After` is only expected on rejected responses.

## Docker Compose Run

The local Docker Compose setup runs the gateway, sample backend, and Redis:

```powershell
docker compose up --build
```

Docker checks:

```powershell
curl.exe -i http://localhost:8080/health/ready
curl.exe -i "http://localhost:8080/api/hello?source=compose"
```

Compose runs RateShield in Redis mode by default:

```text
RateShield__Storage__Mode=Redis
RateShield__Redis__ConnectionString=redis:6379
```

Inside Docker, the gateway forwards to the sample backend using the Compose service name:

```text
http://sample-backend:8080/
```

Inside Docker, the gateway connects to Redis using the Compose service name:

```text
redis:6379
```

The Redis port is also exposed to the host for local inspection:

```text
localhost:6379
```

Do not use `localhost` for container-to-container forwarding. Inside a container, `localhost` means that same container.

## Ports

```text
5011 -> local gateway
5255 -> local sample backend
8080 -> Docker Compose gateway
8081 -> Docker Compose sample backend (direct benchmark access)
6379 -> Docker Compose Redis
```

## Notes

Only YARP-matched proxy routes are rate limited. Gateway-owned endpoints such as `/health/live` and `/health/ready` bypass rate limiting so health checks do not consume client quota.
