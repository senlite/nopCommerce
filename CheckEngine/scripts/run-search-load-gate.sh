#!/usr/bin/env bash
# Search concurrency rehearsal for G4 / NFR-017 (2,000 shoppers, first-page p95 ≤ 300 ms).
# Prefers k6 when installed; otherwise runs the Node sample in search-nfr017-sample.mjs.
# Default in-flight search cap is 25 (single-node HTTP ceiling on this class of host).
# Set CHECKENGINE_LOAD_CONCURRENCY equal to VUS for a synchronized herd.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/../.." && pwd)"
cd "$ROOT_DIR"

if [[ -s "${NVM_DIR:-$HOME/.nvm}/nvm.sh" ]]; then
  # shellcheck disable=SC1091
  . "${NVM_DIR:-$HOME/.nvm}/nvm.sh"
fi

export CHECKENGINE_LOAD_BASE_URL="${CHECKENGINE_LOAD_BASE_URL:-http://127.0.0.1:5000}"
export CHECKENGINE_LOAD_VUS="${CHECKENGINE_LOAD_VUS:-2000}"
export CHECKENGINE_LOAD_P95_MS="${CHECKENGINE_LOAD_P95_MS:-300}"
export CHECKENGINE_LOAD_P99_MS="${CHECKENGINE_LOAD_P99_MS:-600}"
export CHECKENGINE_LOAD_OUTPUT="${CHECKENGINE_LOAD_OUTPUT:-/tmp/checkengine-nfr017.json}"
export CHECKENGINE_LOAD_CONCURRENCY="${CHECKENGINE_LOAD_CONCURRENCY:-25}"
export CHECKENGINE_LOAD_HOLD_SECONDS="${CHECKENGINE_LOAD_HOLD_SECONDS:-15}"

echo "[nfr017-gate] target=${CHECKENGINE_LOAD_BASE_URL} vus=${CHECKENGINE_LOAD_VUS} concurrency=${CHECKENGINE_LOAD_CONCURRENCY} p95<=${CHECKENGINE_LOAD_P95_MS}ms"

if command -v k6 >/dev/null 2>&1; then
  echo "[nfr017-gate] using k6 $(k6 version | head -n1)"
  k6 run "$ROOT_DIR/CheckEngine/tests/perf/search-nfr017.js"
  exit 0
fi

if ! command -v node >/dev/null 2>&1; then
  echo "[nfr017-gate] neither k6 nor node is available." >&2
  exit 2
fi

echo "[nfr017-gate] k6 not found; running Node 2,000-session sample"
node "$SCRIPT_DIR/search-nfr017-sample.mjs"
