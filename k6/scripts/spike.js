import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";

const errorRate = new Rate("errors");
const responseTime = new Trend("response_time", true);

const TARGET_URL = __ENV.TARGET_URL || "http://2.25.122.11:5020";
const peakVus = parseInt(__ENV.VUS_OVERRIDE || "500");

export const options = {
  stages: [
    { duration: "10s", target: 0 },
    { duration: "30s", target: peakVus },
    { duration: "1m",  target: peakVus },
    { duration: "30s", target: 0 },
    { duration: "1m",  target: 0 },
  ],
  thresholds: {
    http_req_duration: ["p(95)<3000"],
    errors: ["rate<0.15"],
  },
};

export default function () {
  let res = http.get(`${TARGET_URL}/health`);
  check(res, { "health OK": (r) => r.status === 200 });
  errorRate.add(res.status !== 200 && res.status !== 429);
  responseTime.add(res.timings.duration);

  res = http.get(`${TARGET_URL}/api/products`);
  check(res, { "products 2xx": (r) => r.status >= 200 && r.status < 300 });
  errorRate.add(res.status !== 200 && res.status !== 429);
  responseTime.add(res.timings.duration);

  const payload = JSON.stringify({
    userId: `spike-user-${__VU}`,
    items: [{ productId: "prod-1", quantity: 1 }],
  });
  res = http.post(`${TARGET_URL}/api/orders`, payload, {
    headers: { "Content-Type": "application/json" },
  });
  check(res, { "order 2xx": (r) => r.status >= 200 && r.status < 300 });
  errorRate.add(res.status !== 200 && res.status !== 429);
  responseTime.add(res.timings.duration);

  sleep(0.2);
}
