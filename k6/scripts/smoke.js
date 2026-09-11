import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.2/index.js";

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

export function handleSummary(data) {
  return {
    "k6/results/summary.json": JSON.stringify(data, null, 2),
    stdout: textSummary(data, { indent: "  ", enableColors: false }),
  };
}

export default function () {
  // Health check
  let res = http.get(`${TARGET_URL}/health/live`);
  check(res, { "health OK": (r) => r.status === 200 });
  errorRate.add(res.status !== 200 && res.status !== 429);
  responseTime.add(res.timings.duration);
  sleep(1);

  // Busca de produtos
  res = http.get(`${TARGET_URL}/api/search?q=notebook&page=1&pageSize=5`);
  check(res, { "search 2xx": (r) => r.status >= 200 && r.status < 300 });
  errorRate.add(res.status !== 200 && res.status !== 429);
  responseTime.add(res.timings.duration);
  sleep(1);

  // Cart — cria e verifica
  res = http.get(`${TARGET_URL}/api/cart/smoke-session`);
  errorRate.add(res.status !== 200 && res.status !== 404 && res.status !== 429);
  responseTime.add(res.timings.duration);
  sleep(1);
}
