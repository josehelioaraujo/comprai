import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";

const errorRate = new Rate("errors");
const responseTime = new Trend("response_time", true);

const TARGET_URL = __ENV.TARGET_URL || "http://2.25.122.11:5020";

export const options = {
  vus: parseInt(__ENV.VUS_OVERRIDE || "3"),
  duration: __ENV.DURATION_OVERRIDE || "30s",
  thresholds: {
    http_req_duration: ["p(95)<500"],
    errors: ["rate<0.01"],
  },
};

export default function () {
  let res = http.get(`${TARGET_URL}/health`);
  check(res, { "health OK": (r) => r.status === 200 });
  errorRate.add(res.status !== 200 && res.status !== 429);
  responseTime.add(res.timings.duration);
  sleep(1);

  res = http.get(`${TARGET_URL}/api/products`);
  check(res, { "products 2xx": (r) => r.status >= 200 && r.status < 300 });
  errorRate.add(res.status !== 200 && res.status !== 429);
  responseTime.add(res.timings.duration);
  sleep(1);

  res = http.get(`${TARGET_URL}/api/orders/smoke-test`);
  errorRate.add(res.status !== 200 && res.status !== 429);
  responseTime.add(res.timings.duration);
  sleep(1);
}
