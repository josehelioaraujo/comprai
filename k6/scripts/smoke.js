import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.2/index.js";

const errorRate  = new Rate("errors");
const rateLimited = new Rate("rate_limited");
const responseTime = new Trend("response_time", true);

const TARGET_URL = __ENV.TARGET_URL || "http://2.25.122.11:5020";

export const options = {
  stages: [
    { duration: "10s", target: 3 },
    { duration: "20s", target: 3 },
    { duration: "10s", target: 0 },
  ],
  thresholds: {
    http_req_duration: ["p(95)<1500"],
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

function addChecks(res, tag) {
  const ok = res.status >= 200 && res.status < 300;
  const is429 = res.status === 429;
  check(res, { [`${tag} 2xx`]: (r) => ok || is429 });
  errorRate.add(!ok && !is429);
  rateLimited.add(is429);
  responseTime.add(res.timings.duration);
}

export default function () {
  const sessionId = `smoke-vu${__VU}-iter${__ITER}`;
  const headers   = { "Content-Type": "application/json" };

  // 1. Health
  let res = http.get(`${TARGET_URL}/health/live`);
  check(res, { "health OK": (r) => r.status === 200 });
  errorRate.add(res.status !== 200 && res.status !== 429);
  rateLimited.add(res.status === 429);
  responseTime.add(res.timings.duration);
  sleep(0.3);

  // 2. Search
  res = http.get(`${TARGET_URL}/api/search?q=notebook&page=1&pageSize=3`);
  addChecks(res, "search");
  let productId = "mock-product-1";
  let productName = "Notebook Smoke";
  let productPrice = 2999.90;
  if (res.status === 200) {
    try {
      const body = JSON.parse(res.body);
      const items = body.items || body.products || body.data || body;
      if (Array.isArray(items) && items.length > 0) {
        productId    = items[0].id    || productId;
        productName  = items[0].name  || items[0].title || productName;
        productPrice = items[0].price || productPrice;
      }
    } catch (_) {}
  }
  sleep(0.3);

  // 3. Add to Cart
  res = http.post(
    `${TARGET_URL}/api/cart/${sessionId}/items`,
    JSON.stringify({ productId, name: productName, price: productPrice, quantity: 1 }),
    { headers }
  );
  addChecks(res, "cart-add");
  sleep(0.3);

  // 4. Get Cart
  res = http.get(`${TARGET_URL}/api/cart/${sessionId}`);
  addChecks(res, "cart-get");
  sleep(0.3);

  // 5. Checkout
  res = http.post(
    `${TARGET_URL}/api/checkout/${sessionId}`,
    JSON.stringify({
      customerName:  "Smoke User",
      customerEmail: `smoke-vu${__VU}@test.com`,
      address:       "Rua Smoke, 1",
    }),
    { headers }
  );
  addChecks(res, "checkout");
  let orderId = null;
  if (res.status === 200 || res.status === 201) {
    try { orderId = JSON.parse(res.body).orderId || JSON.parse(res.body).id; } catch (_) {}
  }
  sleep(0.3);

  // 6. Payment
  if (orderId) {
    res = http.post(
      `${TARGET_URL}/api/payment/${orderId}`,
      JSON.stringify({ method: "mock", amount: productPrice }),
      { headers }
    );
    addChecks(res, "payment");
    sleep(0.3);

    // 7. Order status
    res = http.get(`${TARGET_URL}/api/orders/${orderId}`);
    addChecks(res, "order");
    sleep(0.3);
  }

  sleep(0.5);
}
