import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.2/index.js";

const errorRate   = new Rate("errors");
const rateLimited = new Rate("rate_limited");
const responseTime = new Trend("response_time", true);
const TARGET_URL  = __ENV.TARGET_URL || "http://2.25.122.11:5020";

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

function track(res, tag) {
  const ok    = res.status >= 200 && res.status < 300;
  const is429 = res.status === 429;
  check(res, { [`${tag} 2xx`]: () => ok || is429 });
  errorRate.add(!ok && !is429);
  rateLimited.add(is429);
  responseTime.add(res.timings.duration);
}

export default function () {
  const sid     = `smoke-vu${__VU}-i${__ITER}`;
  const headers = { "Content-Type": "application/json" };

  // 1. Health
  let res = http.get(`${TARGET_URL}/health/live`);
  check(res, { "health OK": (r) => r.status === 200 });
  errorRate.add(res.status !== 200 && res.status !== 429);
  rateLimited.add(res.status === 429);
  responseTime.add(res.timings.duration);
  sleep(0.3);

  // 2. Search — extrair produto real
  res = http.get(`${TARGET_URL}/api/search?q=notebook&page=1&pageSize=3`);
  track(res, "search");
  let productId    = `mock-${sid}`;
  let productTitle = "Notebook Smoke Test";
  let productPrice = 2999.90;
  let productCat   = "eletronicos";
  if (res.status === 200) {
    try {
      const body  = JSON.parse(res.body);
      const items = body.items || body.products || body.data || body;
      if (Array.isArray(items) && items.length > 0) {
        const p       = items[0];
        productId    = p.id    || productId;
        productTitle = p.title || p.name || productTitle;
        productPrice = p.price || productPrice;
        productCat   = p.category || productCat;
      }
    } catch (_) {}
  }
  sleep(0.3);

  // 3. Add to Cart — ProductDto aninhado
  res = http.post(
    `${TARGET_URL}/api/cart/${sid}/items`,
    JSON.stringify({
      product: {
        id:       productId,
        title:    productTitle,
        price:    productPrice,
        category: productCat,
        source:   "k6-smoke",
        imageUrl: null,
        url:      null,
      },
      quantity: 1,
    }),
    { headers }
  );
  track(res, "cart-add");
  sleep(0.3);

  // 4. Get Cart
  res = http.get(`${TARGET_URL}/api/cart/${sid}`);
  track(res, "cart-get");
  sleep(0.3);

  // 5. Checkout — CustomerDto: Name, Email, Phone, Address
  res = http.post(
    `${TARGET_URL}/api/checkout/${sid}`,
    JSON.stringify({
      name:    `Smoke User VU${__VU}`,
      email:   `smoke-vu${__VU}@k6.test`,
      phone:   "11999999999",
      address: "Rua Smoke, 1, São Paulo, SP",
    }),
    { headers }
  );
  track(res, "checkout");
  let orderId = null;
  if (res.status === 200) {
    try {
      const body = JSON.parse(res.body);
      orderId = body.orderId || body.id || null;
    } catch (_) {}
  }
  sleep(0.3);

  // 6. Payment — PaymentRequestDto: Amount, Currency, Method.Provider
  if (orderId) {
    res = http.post(
      `${TARGET_URL}/api/payment/${orderId}`,
      JSON.stringify({
        amount:   productPrice,
        currency: "BRL",
        method: {
          provider:  "mock",
          cardToken: null,
          pixKey:    null,
        },
      }),
      { headers }
    );
    track(res, "payment");
    sleep(0.3);

    // 7. Order status
    res = http.get(`${TARGET_URL}/api/orders/${orderId}`);
    track(res, "order");
    sleep(0.3);
  }

  sleep(0.5);
}
