import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.2/index.js";

const errorRate   = new Rate("errors");
const rateLimited = new Rate("rate_limited");
const responseTime = new Trend("response_time", true);
const TARGET_URL  = __ENV.TARGET_URL || "http://2.25.122.11:5020";
const peakVus     = parseInt(__ENV.VUS_OVERRIDE || "50");

export const options = {
  stages: [
    { duration: "30s", target: peakVus },
    { duration: "3m",  target: peakVus },
    { duration: "30s", target: 0 },
  ],
  thresholds: {
    http_req_duration: ["p(95)<2000"],
    errors: ["rate<0.05"],
  },
  summaryTrendStats: ["avg", "min", "med", "max", "p(90)", "p(95)", "p(99)"],
};

export function handleSummary(data) {
  const dir = __ENV.RESULTS_DIR || "k6/results";
  return {
    [dir + "/summary.json"]: JSON.stringify(data, null, 2),
    stdout: textSummary(data, { indent: "  ", enableColors: false }),
  };
}

function track(res, tag) {
  const ok = res.status >= 200 && res.status < 300;
  const is429 = res.status === 429;
  check(res, { [`${tag} 2xx`]: () => ok || is429 });
  errorRate.add(!ok && !is429);
  rateLimited.add(is429);
  responseTime.add(res.timings.duration);
}

const QUERIES = ["notebook", "smartphone", "fone", "monitor", "mouse"];

export default function () {
  const sid     = `soak-vu${__VU}-i${__ITER}`;
  const headers = { "Content-Type": "application/json" };
  const q       = QUERIES[__ITER % QUERIES.length];

  let res = http.get(`${TARGET_URL}/api/search?q=${q}&page=1&pageSize=5`);
  track(res, "search");
  let productId = `mock-${sid}`; let productTitle = "Produto Soak"; let productPrice = 799.90; let productCat = "geral";
  if (res.status === 200) {
    try {
      const items = JSON.parse(res.body).items || JSON.parse(res.body).products || JSON.parse(res.body);
      if (Array.isArray(items) && items.length > 0) {
        const p = items[0];
        productId = p.id || productId; productTitle = p.title || p.name || productTitle;
        productPrice = p.price || productPrice; productCat = p.category || productCat;
      }
    } catch (_) {}
  }
  sleep(0.5);

  res = http.post(`${TARGET_URL}/api/cart/${sid}/items`,
    JSON.stringify({ product: { id: productId, title: productTitle, price: productPrice, category: productCat, source: "k6-soak", imageUrl: null, url: null }, quantity: 1 }),
    { headers });
  track(res, "cart-add");
  sleep(0.5);

  res = http.get(`${TARGET_URL}/api/cart/${sid}`);
  track(res, "cart-get");
  sleep(0.5);

  res = http.post(`${TARGET_URL}/api/checkout/${sid}`,
    JSON.stringify({ name: `Soak User VU${__VU}`, email: `soak-vu${__VU}@k6.test`, phone: "11955554444", address: "Av. Soak, 50, São Paulo, SP" }),
    { headers });
  track(res, "checkout");
  let orderId = null;
  if (res.status === 200) { try { orderId = JSON.parse(res.body).orderId || null; } catch (_) {} }
  sleep(0.5);

  if (orderId) {
    res = http.post(`${TARGET_URL}/api/payment/${orderId}`,
      JSON.stringify({ amount: productPrice, currency: "BRL", method: { provider: "mock", cardToken: null, pixKey: null } }),
      { headers });
    track(res, "payment");
    sleep(0.5);
    res = http.get(`${TARGET_URL}/api/orders/${orderId}`);
    track(res, "order");
    sleep(0.5);
  }

  sleep(1);
}
