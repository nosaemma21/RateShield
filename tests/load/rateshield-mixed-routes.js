import http from "k6/http";
import { check } from "k6";
import { Counter } from "k6/metrics";

const baseUrl = __ENV.BASE_URL || "http://localhost:8080";
const defaultRejections = new Counter("default_route_rejections");
const strictRejections = new Counter("strict_route_rejections");

export const options = {
  discardResponseBodies: true,

  scenarios: {
    defaultPolicy: {
      executor: "shared-iterations",
      exec: "defaultPolicyTraffic",
      vus: 25,
      iterations: 50,
      maxDuration: "30s",
    },

    strictPolicy: {
      executor: "shared-iterations",
      exec: "strictPolicyTraffic",
      vus: 25,
      iterations: 50,
      maxDuration: "30s",
    },
  },

  thresholds: {
    checks: ["rate == 1"],
    default_route_rejections: ["count == 0"],
    strict_route_rejections: ["count > 0"],
  },
};

function getHeader(response, headerName) {
  const matchingName = Object.keys(response.headers).find(
    (name) => name.toLowerCase() === headerName.toLowerCase(),
  );

  return matchingName ? response.headers[matchingName] : undefined;
}

function sendRequest(path, expectedLimit, rejectionCounter) {
  const response = http.get(`${baseUrl}${path}`, {
    headers: {
      "X-Client-Id": "mixed-route-client",
    },
  });

  if (response.status === 429) {
    rejectionCounter.add(1);
  }

  check(response, {
    "status is 200 or 429": (result) =>
      result.status === 200 || result.status === 429,

    [`route reports limit ${expectedLimit}`]: (result) =>
      getHeader(result, "X-RateLimit-Limit") === String(expectedLimit),
  });
}

export function defaultPolicyTraffic() {
  sendRequest("/api/hello", 100, defaultRejections);
}

export function strictPolicyTraffic() {
  sendRequest("/api/strict/hello", 20, strictRejections);
}
