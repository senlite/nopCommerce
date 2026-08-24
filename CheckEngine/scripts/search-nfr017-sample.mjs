#!/usr/bin/env node
// HTTP sample for NFR-017 when k6 is not installed.
// Opens CHECKENGINE_LOAD_VUS guest sessions (default 2000), then POSTs first-page search.
// CHECKENGINE_LOAD_CONCURRENCY caps in-flight searches (default = VUS, all at once).
// A browser User-Agent is required: nopCommerce maps crawler UAs (curl, k6, undici) onto one
// built-in search-engine customer, which would collapse the sample onto a single rate-limit key.
import { writeFileSync } from 'node:fs';
import http from 'node:http';
import https from 'node:https';
import { URL } from 'node:url';

const BASE_URL = (process.env.CHECKENGINE_LOAD_BASE_URL || 'http://127.0.0.1:5000').replace(/\/$/, '');
const VUS = Number(process.env.CHECKENGINE_LOAD_VUS || 2000);
const P95_MS = Number(process.env.CHECKENGINE_LOAD_P95_MS || 300);
const P99_MS = Number(process.env.CHECKENGINE_LOAD_P99_MS || 600);
const OPEN_BATCH = Number(process.env.CHECKENGINE_LOAD_OPEN_BATCH || 50);
const CONCURRENCY = Number(process.env.CHECKENGINE_LOAD_CONCURRENCY || 25);
const OUTPUT = process.env.CHECKENGINE_LOAD_OUTPUT || '/tmp/checkengine-nfr017.json';
const QUERIES = ['filter', 'oil', 'brake', 'filter oil', 'oem'];
const USER_AGENT = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36';

const base = new URL(BASE_URL);
const transport = base.protocol === 'https:' ? https : http;
const agent = new transport.Agent({
  keepAlive: true,
  maxSockets: Math.max(256, CONCURRENCY, VUS),
  maxFreeSockets: 256,
  timeout: 60_000
});

function request(method, path, { headers = {}, body = null } = {}) {
  return new Promise((resolve) => {
    const payload = body == null ? null : Buffer.from(body);
    const req = transport.request({
      protocol: base.protocol,
      hostname: base.hostname,
      port: base.port || (base.protocol === 'https:' ? 443 : 80),
      path,
      method,
      agent,
      headers: {
        ...headers,
        ...(payload ? { 'Content-Length': Buffer.byteLength(payload) } : {})
      }
    }, (res) => {
      const chunks = [];
      res.on('data', (chunk) => chunks.push(chunk));
      res.on('end', () => {
        resolve({
          status: res.statusCode ?? 0,
          setCookie: res.headers['set-cookie'] || [],
          body: Buffer.concat(chunks).toString('utf8'),
          error: null
        });
      });
    });
    req.on('error', (error) => {
      resolve({ status: 0, setCookie: [], body: '', error: error.code || error.message });
    });
    if (payload)
      req.write(payload);
    req.end();
  });
}

function mergeCookies(existing, setCookie) {
  const map = new Map();
  for (const part of existing.split(';').map((item) => item.trim()).filter(Boolean)) {
    const index = part.indexOf('=');
    if (index > 0)
      map.set(part.slice(0, index), part.slice(index + 1));
  }
  const values = Array.isArray(setCookie) ? setCookie : setCookie ? [setCookie] : [];
  for (const entry of values) {
    const pair = String(entry).split(';')[0];
    const index = pair.indexOf('=');
    if (index <= 0)
      continue;
    const name = pair.slice(0, index).trim();
    const value = pair.slice(index + 1).trim();
    if (value)
      map.set(name, value);
    else
      map.delete(name);
  }
  return [...map.entries()].map(([name, value]) => `${name}=${value}`).join('; ');
}

function customerGuid(cookie) {
  const match = /(?:^|;\s*)\.Nop\.Customer=([^;]+)/.exec(cookie);
  return match ? match[1] : '';
}

async function openGuestSession(index) {
  const response = await request('GET', '/', { headers: { 'User-Agent': USER_AGENT } });
  const cookie = mergeCookies('', response.setCookie);
  return {
    index,
    cookie,
    customerGuid: customerGuid(cookie),
    homeStatus: response.status
  };
}

async function search(session) {
  const rawText = QUERIES[session.index % QUERIES.length];
  const started = performance.now();
  const response = await request('POST', '/check-engine/search/query', {
    headers: {
      'Content-Type': 'application/json',
      'User-Agent': USER_AGENT,
      ...(session.cookie ? { Cookie: session.cookie } : {})
    },
    body: JSON.stringify({
      rawText,
      mode: 6,
      page: 1,
      pageSize: 24,
      locale: 'en'
    })
  });
  return {
    index: session.index,
    status: response.status,
    elapsedMs: performance.now() - started,
    rateLimited: response.status === 429,
    ok: response.status === 200,
    error: response.error,
    bodyLength: response.body.length
  };
}

async function mapPool(items, concurrency, mapper) {
  const results = new Array(items.length);
  let next = 0;
  async function worker() {
    while (next < items.length) {
      const current = next;
      next += 1;
      results[current] = await mapper(items[current], current);
    }
  }
  await Promise.all(Array.from(
    { length: Math.min(Math.max(1, concurrency), items.length) },
    () => worker()));
  return results;
}

function percentile(sorted, ratio) {
  if (sorted.length === 0)
    return 0;
  const index = Math.min(sorted.length - 1, Math.max(0, Math.ceil(sorted.length * ratio) - 1));
  return sorted[index];
}

const wallStart = performance.now();
console.log(`[nfr017] opening ${VUS} guest sessions against ${BASE_URL}`);
const sessions = [];
for (let offset = 0; offset < VUS; offset += OPEN_BATCH) {
  const size = Math.min(OPEN_BATCH, VUS - offset);
  const chunk = await Promise.all(
    Array.from({ length: size }, (_, inner) => openGuestSession(offset + inner)));
  sessions.push(...chunk);
  console.log(`[nfr017] opened ${sessions.length}/${VUS} guest sessions`);
}

const uniqueShoppers = new Set(sessions.map((session) => session.customerGuid).filter(Boolean)).size;
console.log(`[nfr017] unique .Nop.Customer cookies=${uniqueShoppers}`);
if (uniqueShoppers < Math.ceil(VUS * 0.95)) {
  console.error('[nfr017] guest cookies collapsed; check User-Agent / crawler mapping');
  process.exit(1);
}

const warmup = Math.min(8, sessions.length);
console.log(`[nfr017] warmup ${warmup} searches`);
for (let i = 0; i < warmup; i++)
  await search(sessions[i]);

console.log(`[nfr017] firing ${VUS} first-page searches (concurrency=${CONCURRENCY})`);
const results = await mapPool(sessions, CONCURRENCY, (session) => search(session));
const wallMs = performance.now() - wallStart;

const durations = results.map((row) => row.elapsedMs).sort((a, b) => a - b);
const ok = results.filter((row) => row.ok).length;
const limited = results.filter((row) => row.rateLimited).length;
const failed = results.length - ok;
const statusCounts = results.reduce((acc, row) => {
  const key = row.error ? `err:${row.error}` : String(row.status);
  acc[key] = (acc[key] || 0) + 1;
  return acc;
}, {});
const summary = {
  nfr: 'NFR-017',
  baseUrl: BASE_URL,
  shoppers: VUS,
  uniqueShoppers,
  concurrency: CONCURRENCY,
  wallMs,
  ok,
  failed,
  rateLimited: limited,
  statusCounts,
  p50Ms: percentile(durations, 0.50),
  p95Ms: percentile(durations, 0.95),
  p99Ms: percentile(durations, 0.99),
  maxMs: durations[durations.length - 1] || 0,
  budgets: { p95Ms: P95_MS, p99Ms: P99_MS },
  passed: failed / results.length < 0.05
    && uniqueShoppers >= Math.ceil(VUS * 0.95)
    && percentile(durations, 0.95) <= P95_MS
    && percentile(durations, 0.99) <= P99_MS
};

writeFileSync(OUTPUT, JSON.stringify(summary, null, 2));
console.log(`[nfr017] ok=${ok} failed=${failed} 429=${limited} unique=${uniqueShoppers} statuses=${JSON.stringify(statusCounts)} p50=${summary.p50Ms.toFixed(1)}ms p95=${summary.p95Ms.toFixed(1)}ms p99=${summary.p99Ms.toFixed(1)}ms wall=${wallMs.toFixed(0)}ms`);
console.log(`[nfr017] wrote ${OUTPUT}`);

if (!summary.passed) {
  console.error('[nfr017] sample exceeded NFR-001/NFR-017 budgets or error rate');
  process.exit(1);
}
console.log('[nfr017] 2,000-session sample within NFR-001 p95/p99');
