import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.2/index.js";

const errorRate = new Rate("errors");
const rateLimited = new Rate("rate_limited");
const responseTime = new Trend("response_time", true);

const TARGET_URL = __ENV.TARGET_URL || "http://2.25.122.11:5020";
const peakVus = parseInt(__ENV.PEAK_VUS || __ENV.VUS_OVERRIDE || "400");

export const options = {
  stages: [
    { duration: "1m",  target: Math.round(peakVus * 0.25) },
    { duration: "2m",  target: Math.round(peakVus * 0.50) },
    { duration: "2m",  target: Math.round(peakVus * 0.75) },
    { duration: "2m",  target: peakVus },
    { duration: "1m",  target: Math.round(peakVus * 0.50) },
    { duration: "30s", target: 0 },
  ],
  thresholds: {
    http_req_duration: ["p(95)<2000"],
    errors: ["rate<0.10"],
  },
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(90)', 'p(95)', 'p(99)'],
};

export function handleSummary(data) {
  const dir = __ENV.RESULTS_DIR || "k6/results";
  return {
    [dir + "/summary.json"]: JSON.stringify(data, null, 2),
    stdout: textSummary(data, { indent: "  ", enableColors: false }),
  };
}

export default function () {
  // Health check
  let res = http.get(`${TARGET_URL}/health/live`);
  check(res, { "health OK": (r) => r.status === 200 });
  errorRate.add(res.status !== 200 && res.status !== 429);
  rateLimited.add(res.status === 429);
  responseTime.add(res.timings.duration);

  // Busca de produtos
  res = http.get(`${TARGET_URL}/api/search?q=notebook&page=1&pageSize=5`);
  check(res, { "search 2xx": (r) => r.status >= 200 && r.status < 300 });
  errorRate.add(res.status !== 200 && res.status !== 429);
  rateLimited.add(res.status === 429);
  responseTime.add(res.timings.duration);

  // Cart — cria sessão por VU
  res = http.get(`${TARGET_URL}/api/cart/stress-user-${__VU}`);
  check(res, { "cart 2xx/404/429": (r) => r.status === 200 || r.status === 404 || r.status === 429 });
  errorRate.add(res.status !== 200 && res.status !== 404 && res.status !== 429);
  rateLimited.add(res.status === 429);
  responseTime.add(res.timings.duration);

  sleep(0.5);
}
