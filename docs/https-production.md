# HTTPS Production Guidance

RateShield should be served over HTTPS in production.

## Render Hosting

On Render, HTTPS terminates at Render's edge.

That means public traffic flows like this:

```text
Client
-> HTTPS
-> Render edge/load balancer
-> HTTP inside Render network
-> RateShield container
```

RateShield does not need to bind HTTPS inside the container unless the hosting platform specifically requires it.

## Container Binding

For Render Docker deployments, RateShield should listen on HTTP inside the container:

```text
http://0.0.0.0:8080
```

The public HTTPS certificate is managed by Render.

## Security Rule

Do not expose RateShield publicly over plain HTTP in production.

Plain HTTP is acceptable only for:

```text
local development
internal container traffic behind HTTPS termination
trusted private network traffic
```

## Forwarded Protocol Headers

When RateShield is behind a proxy, the upstream proxy may send:

```text
X-Forwarded-Proto: https
```

Only trust forwarded headers from configured trusted proxies.

## Production Checklist

Before production:

```text
Confirm public URL uses HTTPS
Confirm Render manages TLS certificate
Confirm container listens on expected internal HTTP port
Confirm RateShield is not exposed directly over public HTTP
Confirm trusted proxy behavior before trusting forwarded headers
```
