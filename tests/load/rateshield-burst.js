import http from "k6/http";
import { check } from "k6";
import { Counter } from "k6/metrics";

const rateLimitedResponse = new Counter("rate_limited_responses");
const baseUrl = __ENV.BASE_URL || "http://localhost:8080";

export const options = {
  discardResponseBodies: true,

  scenarios: {
    burst: {
      executor: "shared-iterations",
      vus: 20,
      iterations: 200,
      maxDuration: "30s",
    },
  },

  thresholds: {
    checks: ["rate == 1"],
    rate_limited_responses: ["count > 0"],
  },
};

export default function () {
  const response = http.get(`${baseUrl}/api/hello`, {
    headers: {
      "X-Client-Id": "burst-test-client",
    },
  });

  if (response.status == 429) {
    rateLimitedResponse.add(1);
  }

  check(response, {
    "request is allowed or rate limited": (result) =>
      result.status === 200 || result.status === 429,

    "429 includes Retry-After": (result) =>
      result.status !== 429 || Boolean(result.headers["Retry-After"]),
  });
}
