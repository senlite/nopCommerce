#!/usr/bin/env bash
# Core Web Vitals rehearsal gate for Check Engine storefront templates (H1.28 / NFR-054).
# Requires Node.js and Lighthouse CLI: npm install -g lighthouse
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

BASE_URL="${CHECKENGINE_CWV_BASE_URL:-http://127.0.0.1:5000}"
OUTPUT_DIR="${CHECKENGINE_CWV_OUTPUT_DIR:-/tmp/checkengine-cwv}"
MAX_LCP_MS="${CHECKENGINE_CWV_MAX_LCP_MS:-2500}"
MAX_SEARCH_LCP_MS="${CHECKENGINE_CWV_MAX_SEARCH_LCP_MS:-1500}"
MAX_INP_MS="${CHECKENGINE_CWV_MAX_INP_MS:-200}"
MAX_CLS="${CHECKENGINE_CWV_MAX_CLS:-0.1}"

mkdir -p "$OUTPUT_DIR"

if ! command -v lighthouse >/dev/null 2>&1; then
  echo "[cwv-gate] lighthouse CLI not found. Install with: npm install -g lighthouse" >&2
  exit 2
fi

if [[ -n "${CHECKENGINE_CWV_PAGES:-}" ]]; then
  IFS=',' read -r -a PAGES <<< "$CHECKENGINE_CWV_PAGES"
else
  PAGES=(
    "/"
    "/search?q=filter"
    "/computers"
    "/en/gmaster-bmw-parts-5"
    "/build-your-own-computer"
    "/en/gmaster-gm-11127548196-2"
    "/ar/"
  )
fi

FORM_FACTOR="${CHECKENGINE_CWV_FORM_FACTOR:-mobile}"
THROTTLE_CPU="${CHECKENGINE_CWV_CPU_SLOWDOWN:-4}"
THROTTLE_RTT="${CHECKENGINE_CWV_RTT_MS:-150}"
THROTTLE_THROUGHPUT="${CHECKENGINE_CWV_THROUGHPUT_KBPS:-1638.4}"

failed=0
for path in "${PAGES[@]}"; do
  url="${BASE_URL%/}${path}"
  safe_name="$(echo "$path" | tr '/?=&' '_' | sed 's/^_*//;s/_*$//')"
  [[ -z "$safe_name" ]] && safe_name="home"
  status="$(curl -s -o /dev/null -w '%{http_code}' -L "$url" || true)"
  if [[ "$status" != "200" ]]; then
    echo "[cwv-gate] ${safe_name}: skipped (HTTP ${status:-unreachable})"
    continue
  fi
  report="${OUTPUT_DIR}/lighthouse-${safe_name}-${FORM_FACTOR}.json"
  echo "[cwv-gate] auditing ${url} (${FORM_FACTOR}, CPU x${THROTTLE_CPU})"
  lighthouse "$url" \
    --quiet \
    --form-factor="$FORM_FACTOR" \
    --screenEmulation.mobile=$([ "$FORM_FACTOR" = "mobile" ] && echo true || echo false) \
    --throttling.cpuSlowdownMultiplier="$THROTTLE_CPU" \
    --throttling.rttMs="$THROTTLE_RTT" \
    --throttling.throughputKbps="$THROTTLE_THROUGHPUT" \
    --chrome-flags="--headless --no-sandbox" \
    --only-categories=performance \
    --output=json \
    --output-path="$report"

  lcp="$(python3 - <<'PY' "$report"
import json, sys
data = json.load(open(sys.argv[1]))
audits = data.get("audits", {})
print(audits.get("largest-contentful-paint", {}).get("numericValue", 99999))
PY
)"
  cls="$(python3 - <<'PY' "$report"
import json, sys
data = json.load(open(sys.argv[1]))
audits = data.get("audits", {})
print(audits.get("cumulative-layout-shift", {}).get("numericValue", 9))
PY
)"
  inp="$(python3 - <<'PY' "$report"
import json, sys
data = json.load(open(sys.argv[1]))
audits = data.get("audits", {})
print(audits.get("interaction-to-next-paint", {}).get("numericValue", audits.get("total-blocking-time", {}).get("numericValue", 99999)))
PY
)"

  page_lcp_budget="$MAX_LCP_MS"
  if [[ "$path" == /search* ]]; then
    page_lcp_budget="$MAX_SEARCH_LCP_MS"
  fi
  echo "[cwv-gate] ${safe_name}: LCP=${lcp}ms CLS=${cls} INP/TBT=${inp}ms (budget LCP<=${page_lcp_budget} INP<=${MAX_INP_MS} CLS<=${MAX_CLS})"
  if python3 - <<PY
lcp = float("$lcp")
inp = float("$inp")
cls = float("$cls")
import sys
sys.exit(0 if lcp <= $page_lcp_budget and inp <= $MAX_INP_MS and cls <= $MAX_CLS else 1)
PY
  then
    echo "[cwv-gate] ${safe_name}: pass"
  else
    echo "[cwv-gate] ${safe_name}: fail" >&2
    failed=1
  fi
done

if [[ "$failed" -ne 0 ]]; then
  echo "[cwv-gate] one or more pages exceeded NFR-054 budgets" >&2
  exit 1
fi

echo "[cwv-gate] all audited pages within NFR-054 budgets"
