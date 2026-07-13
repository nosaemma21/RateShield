import http from "k6/http";
import { check } from "k6";
import exec from "k6/execution";

const baseUrl = __ENV.BASE_URL || "http://localhost:8080";
const runId = __ENV.RUN_ID || "local";

export const options = {
  discardResponseBodies: true,

  scenarios: {
    manyClients: {
      executor: "shared-iterations",
      vus: 50,
      iterations: 1000,
      maxDuration: "30s",
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
      "X-Client-Id": `many-client-${runId}-${clientNumber}`,
    },
  });

  check(response, {
    "new client is allowed": (result) => result.status == 200,
  });
}
