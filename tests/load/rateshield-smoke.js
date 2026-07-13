import http from "k6/http";
import { check } from "k6";

const baseUrl = __ENV.BASE_URL || "http://localhost:8080";

export const options = {
  discardResponseBodies: true,
  scenarios: {
    smoke: {
      executor: "constant-arrival-rate",
      rate: Number(__ENV.RATE || 5),
      timeUnit: "1s",
      duration: __ENV.DURATION || "10s",
      preAllocatedVUs: 2,
      maxVUs: 10,
    },
  },
  thresholds: {
    checks: ["rate == 1"],
    http_req_failed: ["rate < 0.01"],
    http_req_duration: ["p(95) < 500"],
  },
};

export default function () {
  const response = http.get(`${baseUrl}/api/hello`, {
    headers: {
      "X-Client-Id": `load-test-client-${__VU}`,
    },
  });

  check(response, {
    "gateway returns 200": (result) => result.status === 200,
  });
}
