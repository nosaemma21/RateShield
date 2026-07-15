import http from "k6/http";
import { check } from "k6";
import exec from "k6/execution";

const baseUrl = __ENV.BASE_URL || "http://localhost:8082";
const bucketCount = Number(__ENV.BUCKETS || 10000);

export const options = {
  discardResponseBodies: true,

  scenarios: {
    seedBuckets: {
      executor: "shared-iterations",
      exec: "seedBuckets",
      vus: 50,
      iterations: bucketCount,
      maxDuration: "30s",
    },

    trafficDuringCleanup: {
      executor: "constant-arrival-rate",
      exec: "trafficDuringCleanup",
      startTime: "2s",
      rate: 100,
      timeUnit: "1s",
      duration: "15s",
      preAllocatedVUs: 50,
      maxVUs: 50,
    },
  },

  thresholds: {
    checks: ["rate == 1"],
    "http_req_failed{scenario:trafficDuringCleanup}": ["rate < 0.01"],
    "http_req_duration{scenario:trafficDuringCleanup}": ["p(95) < 500"],
    "dropped_iterations{scenario:trafficDuringCleanup}": ["count == 0"],
  },
};

export function seedBuckets() {
  const response = http.get(`${baseUrl}/api/hello`, {
    headers: {
      "X-Client-Id": `cleanup-idle-${exec.scenario.iterationInTest}`,
    },
  });

  check(response, {
    "seed request returns 200": (result) => result.status === 200,
  });
}

export function trafficDuringCleanup() {
  const clientNumber = exec.scenario.iterationInTest % 50;

  const response = http.get(`${baseUrl}/api/hello`, {
    headers: {
      "X-Client-Id": `cleanup-active-${clientNumber}`,
    },
  });

  check(response, {
    "active request returns 200": (result) => result.status === 200,
  });
}
