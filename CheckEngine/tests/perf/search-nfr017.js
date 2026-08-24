// k6 rehearsal for NFR-017: 2,000 concurrent shoppers, first-page search p95 ≤ 300 ms (NFR-001).
// Each VU is one shopper: GET / (no follow) to mint a guest cookie, then POST /check-engine/search/query.
// A browser User-Agent is required so nopCommerce does not map the VU onto the built-in crawler customer.
//
//   k6 run CheckEngine/tests/perf/search-nfr017.js
//   CHECKENGINE_LOAD_BASE_URL=http://127.0.0.1:5000 CHECKENGINE_LOAD_VUS=2000 k6 run ...
import http from 'k6/http';
import { check } from 'k6';

const BASE_URL = (__ENV.CHECKENGINE_LOAD_BASE_URL || 'http://127.0.0.1:5000').replace(/\/$/, '');
const VUS = Number(__ENV.CHECKENGINE_LOAD_VUS || 2000);
const P95_MS = Number(__ENV.CHECKENGINE_LOAD_P95_MS || 300);
const QUERIES = ['filter', 'oil', 'brake', 'filter oil', 'oem'];
const USER_AGENT = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36';
const HEADERS = { 'User-Agent': USER_AGENT };

export const options = {
  scenarios: {
    nfr017_shoppers: {
      executor: 'per-vu-iterations',
      vus: VUS,
      iterations: 1,
      maxDuration: '2m'
    }
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],
    'http_req_duration{name:search}': [`p(95)<${P95_MS}`, 'p(99)<600']
  }
};

export function setup() {
  const headers = { 'User-Agent': USER_AGENT, 'Content-Type': 'application/json' };
  http.get(`${BASE_URL}/`, { redirects: 0, headers: { 'User-Agent': USER_AGENT } });
  for (let i = 0; i < 8; i++) {
    http.post(
      `${BASE_URL}/check-engine/search/query`,
      JSON.stringify({ rawText: QUERIES[i % QUERIES.length], mode: 6, page: 1, pageSize: 24, locale: 'en' }),
      { headers, tags: { name: 'warmup' } }
    );
  }
}

export default function () {
  const home = http.get(`${BASE_URL}/`, {
    redirects: 0,
    headers: HEADERS,
    tags: { name: 'home' }
  });
  check(home, {
    'home issued a guest cookie': (res) =>
      (res.status === 200 || res.status === 302) && String(res.headers['Set-Cookie'] || '').includes('.Nop.Customer=')
  });

  const rawText = QUERIES[Math.floor(Math.random() * QUERIES.length)];
  const response = http.post(
    `${BASE_URL}/check-engine/search/query`,
    JSON.stringify({
      rawText,
      mode: 6,
      page: 1,
      pageSize: 24,
      locale: 'en'
    }),
    {
      headers: { ...HEADERS, 'Content-Type': 'application/json' },
      tags: { name: 'search' }
    }
  );

  check(response, {
    'search is 200': (res) => res.status === 200,
    'search is not rate-limited': (res) => res.status !== 429
  });
}
