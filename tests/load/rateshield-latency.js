import http from "k6/http";
import { check } from "k6";
import { Trend } from "k6/metrics";

const gatewayUrl = __ENV.GATEWAY_URL || "http://localhost:8080";
const backendUrl = __ENV.BACKEND_URL || "http://localhost:8081";

const gatewayDuration = new Trend("gateway_duration", true);
const backendDuration = new Trend("direct_backend_duration", true);

export const options = {
  discardResponseBodies: true,

  scenarios: {
    directBackend: {
      executor: "constant-arrival-rate",
      exec: "directBackendTraffic",
      rate: 10,
      timeUnit: "1s",
      duration: "30s",
      preAllocatedVUs: 2,
      maxVUs: 10,
    },

    throughGateway: {
      executor: "constant-arrival-rate",
      exec: "gatewayTraffic",
      rate: 10,
      timeUnit: "1s",
      duration: "30s",
      preAllocatedVUs: 2,
      maxVUs: 10,
    },
  },
  thresholds: {
    checks: ["rate == 1"],
    direct_backend_duration: ["p(95) < 500"],
    gateway_duration: ["p(95) <  500"],
    dropped_iterations: ["count == 0"],
  },
};

export function directBackendTraffic() {
  const response = http.get(`${backendUrl}/api/hello`);

  backendDuration.add(response.timings.duration);

  check(response, {
    "direct backend returns 200": (result) => result.status === 200,
  });
}

export function gatewayTraffic() {
  const response = http.get(`${gatewayUrl}/api/hello`, {
    headers: {
      "X-Client-Id": "latency-test-client",
    },
  });

  gatewayDuration.add(response.timings.duration);

  check(response, {
    "gateway returns 200": (result) => result.status === 200,
  });
}
