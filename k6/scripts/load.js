import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";

const errorRate = new Rate("errors");
const responseTime = new Trend("response_time", true);

const TARGET_URL = __ENV.TARGET_URL || "http://2.25.122.11:5020";
const peakVus = parseInt(__ENV.VUS_OVERRIDE || "100");

export const options = {
  stages: [
    { duration: "1m",  target: Math.round(peakVus * 0.25) },
    { duration: "2m",  target: Math.round(peakVus * 0.50) },
    { duration: "2m",  target: peakVus },
    { duration: "1m",  target: 0 },
  ],
  thresholds: {
    http_req_duration: ["p(95)<800"],
    errors: ["rate<0.05"],
  },
};

export default function () {
  let res = http.get(`${TARGET_URL}/health`);
  check(res, { "health OK": (r) => r.status === 200 });
  errorRate.add(res.status !== 200 && res.status !== 429);
  responseTime.add(res.timings.duration);
  sleep(0.5);

  res = http.get(`${TARGET_URL}/api/products`);
  check(res, { "products 2xx": (r) => r.status >= 200 && r.status < 300 });
  errorRate.add(res.status !== 200 && res.status !== 429);
  responseTime.add(res.timings.duration);
  sleep(0.5);

  const payload = JSON.stringify({
    userId: `load-user-${__VU}`,
    items: [{ productId: "prod-1", quantity: 1 }],
  });
  res = http.post(`${TARGET_URL}/api/orders`, payload, {
    headers: { "Content-Type": "application/json" },
  });
  check(res, { "order created": (r) => r.status >= 200 && r.status < 300 });
  errorRate.add(res.status !== 200 && res.status !== 429);
  responseTime.add(res.timings.duration);
  sleep(1);
}
