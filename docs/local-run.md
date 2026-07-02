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
curl -i http://localhost:5255/
curl -i "http://localhost:5255/api/hello?source=backend"
curl -i http://localhost:5255/slow
curl -i http://localhost:5255/fail
curl -i http://localhost:5255/headers
curl -i -X POST http://localhost:5255/echo -H "Content-Type: application/json" -d "{\"message\":\"hello\"}"
```

## Run The Gateway

In a second terminal, from the repository root:

```powershell
dotnet run --project src/RateShield.Gateway
```

Gateway checks:

```powershell
curl -i http://localhost:5011/health/live
curl -i http://localhost:5011/health/ready
curl -i "http://localhost:5011/api/hello?source=gateway"
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
curl -i http://localhost:5011/api/hello
curl -i http://localhost:5011/api/hello
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

The local Docker Compose setup runs both the gateway and sample backend:

```powershell
docker compose up --build
```

Docker checks:

```powershell
curl -i http://localhost:8080/health/ready
curl -i "http://localhost:8080/api/hello?source=compose"
```

Inside Docker, the gateway forwards to the sample backend using the Compose service name:

```text
http://sample-backend:8080/
```

Do not use `localhost` for container-to-container forwarding. Inside a container, `localhost` means that same container.

## Ports

```text
5011 -> local gateway
5255 -> local sample backend
8080 -> Docker Compose gateway
```

## Notes

Only YARP-matched proxy routes are rate limited. Gateway-owned endpoints such as `/health/live` and `/health/ready` bypass rate limiting so health checks do not consume client quota.