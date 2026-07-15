import http from "k6/http";
import { check } from "k6";
import exec from "k6/execution";
import { Counter } from "k6/metrics";

const gatewayA = __ENV.GATEWAY_A || "http://localhost:8082";
const gatewayB = __ENV.GATEWAY_B || "http://localhost:8083";

const clientId = __ENV.CLIENT_ID || "multi-instance-shared-client";

const allowedResponses = new Counter("allowed_responses");
const rateLimitedResponses = new Counter("rate_limited_responses");
const gatewayARequests = new Counter("gateway_a_requests");
const gatewayBRequests = new Counter("gateway_b_requests");

http.setResponseCallback(http.expectedStatuses(200, 429));

export const options = {
  discardResponseBodies: true,

  scenarios: {
    sharedRedisBucket: {
      executor: "shared-iterations",
      vus: 20,
      iterations: 200,
      maxDuration: "30s",
    },
  },

  thresholds: {
    checks: ["rate == 1"],
    allowed_responses: ["count >= 100", "count <= 110"],
    rate_limited_responses: ["count >= 90"],
    gateway_a_requests: ["count == 100"],
    gateway_b_requests: ["count == 100"],
  },
};

export default function () {
  const useGatewayA = exec.scenario.iterationInTest % 2 === 0;
  const baseUrl = useGatewayA ? gatewayA : gatewayB;

  if (useGatewayA) {
    gatewayARequests.add(1);
  } else {
    gatewayBRequests.add(1);
  }

  const response = http.get(`${baseUrl}/api/hello`, {
    headers: {
      "X-Client-Id": clientId,
    },
  });

  if (response.status === 200) {
    allowedResponses.add(1);
  }

  if (response.status === 429) {
    rateLimitedResponses.add(1);
  }

  check(response, {
    "request is allowed or rate limited": (result) =>
      result.status === 200 || result.status === 429,

    "429 includes Retry-After": (result) =>
      result.status != 429 || Boolean(result.headers["Retry-After"]),
  });
}
