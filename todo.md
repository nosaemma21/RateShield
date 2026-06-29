# RateShield TODO

This is the ordered execution checklist for RateShield, a production-shaped distributed API gateway rate limiter. It is not a local-only demo checklist. It covers planning, application code, storage, hosting, CI/CD, testing, observability, documentation, and release readiness.

## 0. Locked Project Decisions

- [x] V1 scope: in-memory limiter, route-specific policies, tests, docs, sample backend, Docker-ready local run.
- [x] V2 scope: Redis-backed distributed limiter for multiple gateway instances.
- [x] Target .NET version: .NET 8 LTS.
- [x] Hosting target: Render Web Service using Docker.
- [x] Container registry: GitHub Container Registry for versioned Docker images, with Render pulling the deployed image.
- [x] Redis deployment choice: Render Key Value for hosted Redis-compatible distributed storage.
- [x] Distributed storage failure behavior: fail-closed for protected production routes, with an explicit development-only fail-open option.
- [x] Trusted proxy strategy: trust forwarded headers only from configured Render/load-balancer proxy ranges; never trust arbitrary `X-Forwarded-For`.
- [x] Initial client identity priority order: API key header, custom configured identity header, trusted forwarded IP, remote IP fallback.
- [x] HTTPS termination: Render terminates HTTPS at the edge; RateShield runs HTTP inside the Render service unless platform settings require otherwise.
- [x] Production deploys: require manual approval before production deploy.
- [x] Required environments: local, CI, staging, production.

## 1. Repository And Solution Setup

- [ ] Create `RateShield.sln`.
- [ ] Create `src/RateShield.Core`.
- [ ] Create `src/RateShield.Infrastructure`.
- [ ] Create `src/RateShield.Gateway`.
- [ ] Create `src/RateShield.SampleBackend`.
- [ ] Create `tests/RateShield.Core.Tests`.
- [ ] Create `tests/RateShield.Gateway.Tests`.
- [ ] Create `docs/`.
- [ ] Add `.editorconfig`.
- [ ] Add `.gitignore`.
- [ ] Add `.dockerignore`.
- [ ] Add solution-level `README.md`.
- [ ] Add solution-level `todo.md`.
- [ ] Add project references in the correct direction.
- [ ] Keep core limiter logic out of `Program.cs`.
- [ ] Add nullable reference types.
- [ ] Add analyzers if selected.

## 2. Core Domain Model

- [ ] Create `RateLimitPolicy`.
- [ ] Create `TokenBucketState`.
- [ ] Create `RateLimitRequest`.
- [ ] Create `RateLimitDecision`.
- [ ] Create `RateLimitHeaders`.
- [ ] Create `ClientIdentity`.
- [ ] Create `RoutePolicyBinding`.
- [ ] Create `RetryAfter` calculation model.
- [ ] Support bucket capacity.
- [ ] Support current token count.
- [ ] Support refill tokens.
- [ ] Support refill period.
- [ ] Support request cost.
- [ ] Support policy name.
- [ ] Support route scope.
- [ ] Support last refill timestamp.
- [ ] Support last seen timestamp.
- [ ] Add `IClock` or equivalent testable time abstraction.

## 3. Token-Bucket Engine

- [ ] Implement token refill logic.
- [ ] Cap refill at bucket capacity.
- [ ] Consume tokens only when enough tokens are available.
- [ ] Reject requests when insufficient tokens remain.
- [ ] Calculate remaining tokens.
- [ ] Calculate reset time.
- [ ] Calculate retry-after value.
- [ ] Handle zero or invalid request cost.
- [ ] Handle invalid policy values.
- [ ] Prevent negative token counts.
- [ ] Avoid global locks.
- [ ] Use atomic operations where appropriate.
- [ ] Keep the engine independent from ASP.NET Core.
- [ ] Keep the engine independent from Redis.

## 4. Storage Abstraction And In-Memory Store

- [ ] Create bucket store abstraction.
- [ ] Implement in-memory bucket store.
- [ ] Use `ConcurrentDictionary` for local bucket lookup.
- [ ] Ensure same-client concurrent requests are handled correctly.
- [ ] Track active bucket count.
- [ ] Track last seen timestamp per bucket.
- [ ] Add stale bucket enumeration for cleanup.
- [ ] Make store behavior testable without web host.
- [ ] Keep storage interface compatible with Redis-backed implementation.

## 5. Client Identity Extraction

- [ ] Create `IClientIdentityProvider`.
- [ ] Implement remote IP provider.
- [ ] Implement API key header provider.
- [ ] Implement custom header provider.
- [ ] Implement `X-Forwarded-For` provider.
- [ ] Add trusted proxy configuration before trusting forwarded headers.
- [ ] Prepare bearer token claim provider for later.
- [ ] Add fallback identity strategy.
- [ ] Normalize identity values.
- [ ] Handle missing identity.
- [ ] Handle malformed identity headers.
- [ ] Avoid logging raw API keys or tokens.

## 6. Policy Resolution

- [ ] Create `IRateLimitPolicyResolver`.
- [ ] Load default policy from configuration.
- [ ] Load named policies from configuration.
- [ ] Map routes to policies.
- [ ] Resolve policy before calling limiter.
- [ ] Support route-specific policies.
- [ ] Support global fallback policy.
- [ ] Validate all policies on startup.
- [ ] Fail startup on invalid required policy config.
- [ ] Document policy selection order.

## 7. Configuration System

- [ ] Add `RateShield` configuration section.
- [ ] Add `RateShield:Storage` configuration.
- [ ] Add `RateShield:Policies` configuration.
- [ ] Add route-to-policy mapping configuration.
- [ ] Add `RateShield:Identity` configuration.
- [ ] Add `RateShield:Cleanup` configuration.
- [ ] Add `RateShield:Redis` configuration.
- [ ] Add `RateShield:RejectionResponse` configuration.
- [ ] Add `RateShield:Observability` configuration.
- [ ] Add configuration validation.
- [ ] Add `appsettings.Development.json`.
- [ ] Add production example config.
- [ ] Add environment variable examples.
- [ ] Document every configuration key.

## 8. Gateway Foundation

- [ ] Add ASP.NET Core gateway project.
- [ ] Add YARP package.
- [ ] Configure YARP routes from `appsettings.json`.
- [ ] Configure YARP clusters from `appsettings.json`.
- [ ] Add default local sample route.
- [ ] Verify forwarding for `GET`.
- [ ] Verify forwarding for `POST`.
- [ ] Verify forwarding for `PUT`.
- [ ] Verify forwarding for `PATCH`.
- [ ] Verify forwarding for `DELETE`.
- [ ] Verify path forwarding.
- [ ] Verify query string forwarding.
- [ ] Verify request body forwarding.
- [ ] Verify important request headers are preserved.
- [ ] Verify response headers are returned from upstream.
- [ ] Add `/health/live`.
- [ ] Add `/health/ready`.

## 9. Rate-Limiting Middleware

- [ ] Create rate-limiting middleware.
- [ ] Place middleware before YARP forwarding.
- [ ] Extract client identity.
- [ ] Resolve policy.
- [ ] Create limiter request.
- [ ] Call limiter.
- [ ] Add rate-limit headers on allowed responses where appropriate.
- [ ] Return HTTP `429 Too Many Requests` on rejected requests.
- [ ] Return JSON rejection body.
- [ ] Add `Retry-After` header on rejection.
- [ ] Avoid exposing internal implementation details in errors.
- [ ] Log allowed and rejected decisions at appropriate levels.

## 10. Response Headers

- [ ] Add `X-RateLimit-Limit`.
- [ ] Add `X-RateLimit-Remaining`.
- [ ] Add `X-RateLimit-Reset`.
- [ ] Add `Retry-After` for rejected requests.
- [ ] Decide whether to also use standard `RateLimit-*` headers.
- [ ] Ensure headers are correct for route-specific policies.
- [ ] Document all emitted headers.

## 11. Background Cleanup Worker

- [ ] Create cleanup options.
- [ ] Add hosted background service.
- [ ] Scan in-memory buckets periodically.
- [ ] Remove buckets idle beyond configured expiration.
- [ ] Avoid blocking request processing.
- [ ] Track cleanup run count.
- [ ] Track cleanup removal count.
- [ ] Log cleanup summary.
- [ ] Support cancellation during shutdown.

## 12. Sample Backend

- [ ] Create sample backend project.
- [ ] Add endpoint returning request method, path, query, and selected headers.
- [ ] Add endpoint for slow responses.
- [ ] Add endpoint for simulated failures.
- [ ] Add endpoint for POST body echo.
- [ ] Configure sample backend local port.
- [ ] Document how to run gateway and backend together.

## 13. Observability

- [ ] Add structured logging.
- [ ] Add request allowed counter.
- [ ] Add request rejected counter.
- [ ] Add active bucket gauge.
- [ ] Add cleanup run counter.
- [ ] Add cleanup removal counter.
- [ ] Add limiter error counter.
- [ ] Add storage error counter.
- [ ] Add policy resolution error logs.
- [ ] Add request correlation ID support.
- [ ] Prepare OpenTelemetry metrics.
- [ ] Prepare Prometheus-compatible metrics endpoint if selected.
- [ ] Document observability setup.

## 14. Security

- [ ] Avoid logging `Authorization` headers.
- [ ] Avoid logging full API keys.
- [ ] Redact sensitive configured headers.
- [ ] Do not trust `X-Forwarded-For` unless trusted proxy configuration allows it.
- [ ] Add trusted proxy documentation.
- [ ] Add HTTPS production guidance.
- [ ] Add secure default rejection body.
- [ ] Avoid leaking upstream backend details in gateway-generated errors.
- [ ] Validate request identity source config.
- [ ] Validate Redis connection secret handling.
- [ ] Document secret management strategy for hosted environments.

## 15. Redis Distributed Storage Design

- [ ] Create `docs/redis-design.md`.
- [ ] Define Redis key format.
- [ ] Define Redis bucket value format.
- [ ] Define Redis TTL strategy.
- [ ] Define atomic update strategy.
- [ ] Decide Lua script inputs and outputs.
- [ ] Decide Redis time source strategy.
- [ ] Decide how to handle clock skew.
- [ ] Decide how to handle Redis timeout.
- [ ] Decide fail-open, fail-closed, or local fallback behavior.
- [ ] Document tradeoffs between local and distributed mode.
- [ ] Document Redis memory sizing expectations.
- [ ] Document Redis eviction policy expectations.

## 16. Redis Implementation

- [ ] Add Redis package only when V2 begins.
- [ ] Add Redis connection configuration.
- [ ] Add Redis health check.
- [ ] Implement atomic Lua token-bucket update.
- [ ] Set TTL on idle buckets.
- [ ] Return decision, remaining tokens, retry-after, and reset time from Redis operation.
- [ ] Handle Redis connection failures.
- [ ] Handle Redis command timeouts.
- [ ] Add integration tests with local Redis or test container.
- [ ] Add Redis-specific docs.
- [ ] Add production Redis sizing notes.

## 17. Storage And Infrastructure

- [ ] Support storage mode options: `InMemory`, `Redis`.
- [ ] Add local in-memory storage for V1.
- [ ] Add Redis storage for V2.
- [ ] Define managed Redis setup.
- [ ] Define self-hosted Redis container setup.
- [ ] Define Kubernetes Redis service setup if Kubernetes is selected.
- [ ] Define Redis persistence expectations.
- [ ] Define Redis memory policy.
- [ ] Define Redis eviction policy.
- [ ] Define Redis connection pooling behavior.
- [ ] Define Redis timeout values.
- [ ] Define Redis retry behavior.
- [ ] Define Redis TLS requirements.
- [ ] Define Redis authentication requirements.
- [ ] Add infrastructure documentation for Redis setup.
- [ ] Add example Redis environment variables.
- [ ] Add local Redis Docker Compose service if Docker Compose is chosen.
- [ ] Add hosted Redis setup guide for chosen hosting provider.

## 18. Docker And Local Runtime

- [ ] Add production Dockerfile for gateway.
- [ ] Add Dockerfile for sample backend if needed.
- [ ] Add `.dockerignore`.
- [ ] Add Docker Compose for gateway, sample backend, and Redis.
- [ ] Add local environment variable template.
- [ ] Add production environment variable template.
- [ ] Add health check configuration for containers.
- [ ] Add graceful shutdown settings.
- [ ] Add local smoke test commands.
- [ ] Document all local ports.

## 19. Hosting And Deployment

- [ ] Decide hosting platform.
- [ ] Document how to deploy to selected host.
- [ ] Document how to configure backend cluster destinations in hosted environment.
- [ ] Add readiness check that verifies required dependencies.
- [ ] Add logging configuration for hosted environment.
- [ ] Add reverse proxy/load balancer notes.
- [ ] Add HTTPS/TLS termination notes.
- [ ] Add autoscaling notes.
- [ ] Add horizontal scaling notes.
- [ ] Add deployment rollback notes.
- [ ] Add deployment smoke test checklist.
- [ ] Add staging environment.
- [ ] Add production environment.

## 20. CI Requirements

- [ ] Add GitHub Actions workflow for pull requests.
- [ ] Add GitHub Actions workflow for main branch pushes.
- [ ] Restore .NET dependencies in CI.
- [ ] Build the full solution in CI.
- [ ] Run all unit tests in CI.
- [ ] Run all integration tests in CI.
- [ ] Fail CI on compiler warnings if strict mode is selected.
- [ ] Run formatting check.
- [ ] Run static analysis/analyzers.
- [ ] Run dependency vulnerability scan.
- [ ] Generate test coverage report.
- [ ] Upload test results as CI artifacts.
- [ ] Upload coverage report as CI artifact.
- [ ] Build Docker image in CI.
- [ ] Scan Docker image for vulnerabilities.
- [ ] Require CI to pass before merging.

## 21. CD Requirements

- [ ] Publish Docker image to selected registry.
- [ ] Tag Docker image with commit SHA.
- [ ] Tag Docker image with semantic version or release tag.
- [ ] Deploy main branch builds to staging automatically if selected.
- [ ] Run staging smoke tests after deploy.
- [ ] Require manual approval before production deploy.
- [ ] Deploy release tags to production.
- [ ] Run production smoke tests after deploy.
- [ ] Support rollback to previous stable image.
- [ ] Store secrets securely using GitHub Actions secrets or hosting-provider secrets.
- [ ] Document required CI/CD environment variables.
- [ ] Document deployment workflow.
- [ ] Document rollback workflow.

## 22. Unit Tests

- [ ] Test initial bucket allows request.
- [ ] Test token consumption.
- [ ] Test token refill.
- [ ] Test refill does not exceed capacity.
- [ ] Test rejection when bucket is empty.
- [ ] Test retry-after calculation.
- [ ] Test reset time calculation.
- [ ] Test custom request cost.
- [ ] Test concurrent same-client requests.
- [ ] Test many-client bucket creation.
- [ ] Test stale bucket cleanup.
- [ ] Test invalid policy validation.
- [ ] Test identity providers.
- [ ] Test policy resolver.

## 23. Integration Tests

- [ ] Test gateway forwards allowed request.
- [ ] Test gateway rejects excessive request.
- [ ] Test gateway returns JSON `429` body.
- [ ] Test gateway returns rate-limit headers.
- [ ] Test route-specific policy applies.
- [ ] Test default policy fallback applies.
- [ ] Test sample backend receives expected forwarded request.
- [ ] Test missing identity behavior.
- [ ] Test malformed header behavior.
- [ ] Test storage failure behavior once Redis exists.
- [ ] Test readiness health check.

## 24. Load And Performance Testing

- [ ] Add simple load test script or tool config.
- [ ] Test burst traffic against one client.
- [ ] Test sustained traffic against one client.
- [ ] Test many unique clients.
- [ ] Test mixed routes with different policies.
- [ ] Test same-client high concurrency.
- [ ] Measure gateway latency overhead.
- [ ] Measure memory growth with many buckets.
- [ ] Measure cleanup impact under load.
- [ ] Test multiple gateway instances once Redis exists.
- [ ] Document performance results.

## 25. Documentation

- [ ] Write project overview.
- [ ] Write architecture document.
- [ ] Add architecture diagram.
- [ ] Write token-bucket explanation.
- [ ] Write configuration reference.
- [ ] Write local run guide.
- [ ] Write test guide.
- [ ] Write Docker guide.
- [ ] Write hosting guide.
- [ ] Write CI/CD guide.
- [ ] Write Redis distributed design guide.
- [ ] Write security considerations.
- [ ] Write observability guide.
- [ ] Write known limitations.
- [ ] Add troubleshooting section.
- [ ] Add example curl commands.

## 26. Developer Experience

- [ ] Add one-command local run path if practical.
- [ ] Add sample requests.
- [ ] Add sample failure/rejection examples.
- [ ] Add environment variable examples.
- [ ] Add clean project scripts if useful.
- [ ] Add clear ports table.
- [ ] Add explanation of how to change upstream backend destination.
- [ ] Add explanation of how to add a new rate-limit policy.
- [ ] Add explanation of how to switch storage modes.
- [ ] Add explanation of how to run CI-equivalent checks locally.

## 27. Production Readiness

- [ ] Validate startup configuration.
- [ ] Validate health checks.
- [ ] Validate logging redaction.
- [ ] Validate Docker image runs.
- [ ] Validate hosted deployment starts.
- [ ] Validate readiness endpoint reflects dependency health.
- [ ] Validate Redis unavailable behavior.
- [ ] Validate backend unavailable behavior.
- [ ] Validate rate limits under multiple gateway instances once Redis is implemented.
- [ ] Validate cleanup does not harm active clients.
- [ ] Validate memory does not grow without bounds.
- [ ] Validate documentation matches actual commands.
- [ ] Validate CI/CD pipeline from pull request to release.

## 28. Final Acceptance Criteria

- [ ] Gateway runs locally.
- [ ] Sample backend runs locally.
- [ ] YARP forwards requests to sample backend.
- [ ] Local token-bucket limiter works.
- [ ] Excessive requests return HTTP `429`.
- [ ] Rate-limit headers are present and accurate.
- [ ] Route-specific policies work.
- [ ] Cleanup worker removes stale buckets.
- [ ] Configuration drives policies and identity strategy.
- [ ] Unit tests pass.
- [ ] Integration tests pass.
- [ ] CI pipeline passes.
- [ ] Docker image builds.
- [ ] Staging deployment works if staging is selected.
- [ ] Production deployment path is documented.
- [ ] Docs explain setup, architecture, storage, hosting, CI/CD, and operation.
- [ ] Redis distributed design is documented.
- [ ] Redis distributed implementation is completed if included in selected scope.
- [ ] Project can be shown as a serious portfolio or production-foundation project.
