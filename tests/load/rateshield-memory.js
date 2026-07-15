import http from "k6/http";
import { check } from "k6";
import exec from "k6/execution";

const baseUrl = (__ENV.BASE_URL = "http://localhost:8082");
const runId = __ENV.RUN_ID || "memory-test";
const bucketCount = Number(__ENV.BUCKETS || 10000);
const virtualUsers = Number(__ENV.VUS || 50);

export const options = {
  discardResponseBodies: true,

  scenarios: {
    memoryGrowth: {
      executor: "shared-iterations",
      vus: virtualUsers,
      iterations: bucketCount,
      maxDuration: "60s",
    },
  },

  thresholds: {
    checks: ["rate == 1"],
    http_req_failed: ["rate < 0.01"],
    http_req_duration: ["p(95) < 500"],
  },
};

export default function () {
  const clientNumber = exec.scenario.iterationInTest;

  const response = http.get(`${baseUrl}/api/hello`, {
    headers: {
      "X-Client-Id": `${runId}-${clientNumber}`,
    },
  });

  check(response, {
    "unique client is allowed": (result) => result.status === 200,
  });
}
