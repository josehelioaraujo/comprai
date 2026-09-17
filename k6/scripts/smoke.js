import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.2/index.js";

const errorRate    = new Rate("errors");
const rateLimited  = new Rate("rate_limited");
const responseTime = new Trend("response_time", true);
const TARGET_URL   = __ENV.TARGET_URL || "http://2.25.122.11:5020";

export const options = {
  stages: [
    { duration: "10s", target: 3 },
    { duration: "20s", target: 3 },
    { duration: "10s", target: 0 },
  ],
  thresholds: {
    http_req_duration: ["p(95)<1500"], errors: ["rate<0.05"],
    "http_req_duration{name:GET /health/live}":     [],
    "http_req_duration{name:GET /api/search}":      [],
    "http_req_duration{name:POST /api/cart/items}": [],
    "http_req_duration{name:GET /api/cart}":        [],
    "http_req_duration{name:POST /api/checkout}":   [],
    "http_req_duration{name:POST /api/payment}":    [],
    "http_req_duration{name:GET /api/orders}":      [],
  },
  summaryTrendStats: ["avg", "min", "med", "max", "p(90)", "p(95)", "p(99)"],
};

const ROUTE_DEFS = [
  { tag: "GET /health/live",     method: "GET",  path: "/health/live" },
  { tag: "GET /api/search",      method: "GET",  path: "/api/search" },
  { tag: "POST /api/cart/items", method: "POST", path: "/api/cart/:id/items" },
  { tag: "GET /api/cart",        method: "GET",  path: "/api/cart/:id" },
  { tag: "POST /api/checkout",   method: "POST", path: "/api/checkout/:id" },
  { tag: "POST /api/payment",    method: "POST", path: "/api/payment/:orderId" },
  { tag: "GET /api/orders",      method: "GET",  path: "/api/orders/:orderId" },
];

function buildStructured(data) {
  const m = data.metrics || {};
  function v(n) { return (m[n] && m[n].values) || {}; }
  function r(n, d) { var f = parseFloat(n); return isNaN(f) ? 0 : parseFloat(f.toFixed(d !== undefined ? d : 1)); }
  const dur = v("http_req_duration"), reqs = v("http_reqs"), fail = v("http_req_failed");
  const errRate = r(fail["rate"] || 0, 4);
  const routes = ROUTE_DEFS.map(function(rd) {
    var metricKey = "http_req_duration{name:" + rd.tag + "}";
    var dv = v(metricKey);
    var fv = v("http_req_failed{name:" + rd.tag + "}");
    if (!m[metricKey]) return null;
    return { method: rd.method, path: rd.path,
      p50: r(dv["med"] || 0), p95: r(dv["p(95)"] || 0),
      count: v("http_reqs{name:" + rd.tag + "}")["count"] || 0, err: r(fv["rate"] || 0, 4) };
  }).filter(Boolean);
  return {
    meta: { test_type: "smoke", target_url: __ENV.TARGET_URL || "http://2.25.122.11:5020",
            vus: __ENV.VUS_OVERRIDE || "3", duration: "40s" },
    scenarios: [{ sc: "smoke", p50: r(dur["med"] || 0), p95: r(dur["p(95)"] || 0),
      p99: r(dur["p(99)"] || 0), rps: r(reqs["rate"] || 0),
      total: reqs["count"] || 0, err: errRate, ok: errRate < 0.05 }],
    routes: routes,
  };
}

export function handleSummary(data) {
  const dir = __ENV.RESULTS_DIR || "k6/results";
  const structured = buildStructured(data);
  return {
    [dir + "/summary.json"]: JSON.stringify(data, null, 2),
    [dir + "/smoke.json"]:   JSON.stringify(structured, null, 2),
    stdout: textSummary(data, { indent: "  ", enableColors: false }),
  };
}

function track(res, tag) {
  const ok = res.status >= 200 && res.status < 300, is429 = res.status === 429;
  check(res, { [`${tag} 2xx`]: () => ok || is429 });
  errorRate.add(!ok && !is429); rateLimited.add(is429); responseTime.add(res.timings.duration);
}

export default function () {
  const sid = `smoke-vu${__VU}-i${__ITER}`, headers = { "Content-Type": "application/json" };

  let res = http.get(`${TARGET_URL}/health/live`, { tags: { name: "GET /health/live" } });
  check(res, { "health OK": (r) => r.status === 200 });
  errorRate.add(res.status !== 200 && res.status !== 429);
  rateLimited.add(res.status === 429); responseTime.add(res.timings.duration);
  sleep(0.3);

  res = http.get(`${TARGET_URL}/api/search?q=notebook&page=1&pageSize=3`, { tags: { name: "GET /api/search" } });
  track(res, "search");
  let productId = `mock-${sid}`, productTitle = "Notebook Smoke", productPrice = 2999.90, productCat = "eletronicos";
  if (res.status === 200) { try { const b = JSON.parse(res.body); const items = b.items || b.products || b.data || b;
    if (Array.isArray(items) && items.length > 0) { const p = items[0]; productId = p.id || productId;
      productTitle = p.title || p.name || productTitle; productPrice = p.price || productPrice; productCat = p.category || productCat; }
  } catch (_) {} }
  sleep(0.3);

  res = http.post(`${TARGET_URL}/api/cart/${sid}/items`,
    JSON.stringify({ product: { id: productId, title: productTitle, price: productPrice, category: productCat, source: "k6-smoke", imageUrl: null, url: null }, quantity: 1 }),
    { headers, tags: { name: "POST /api/cart/items" } });
  track(res, "cart-add"); sleep(0.3);

  res = http.get(`${TARGET_URL}/api/cart/${sid}`, { tags: { name: "GET /api/cart" } });
  track(res, "cart-get"); sleep(0.3);

  res = http.post(`${TARGET_URL}/api/checkout/${sid}`,
    JSON.stringify({ name: `Smoke User VU${__VU}`, email: `smoke-vu${__VU}@k6.test`, phone: "11999999999", address: "Rua Smoke, 1, Sao Paulo, SP" }),
    { headers, tags: { name: "POST /api/checkout" } });
  track(res, "checkout");
  let orderId = null;
  if (res.status === 200) { try { const b = JSON.parse(res.body); orderId = b.orderId || b.id || null; } catch (_) {} }
  sleep(0.3);

  if (orderId) {
    res = http.post(`${TARGET_URL}/api/payment/${orderId}`,
      JSON.stringify({ amount: productPrice, currency: "BRL", method: { provider: "mock", cardToken: null, pixKey: null } }),
      { headers, tags: { name: "POST /api/payment" } });
    track(res, "payment"); sleep(0.3);

    res = http.get(`${TARGET_URL}/api/orders/${orderId}`, { tags: { name: "GET /api/orders" } });
    track(res, "order"); sleep(0.3);
  }
  sleep(0.5);
}
