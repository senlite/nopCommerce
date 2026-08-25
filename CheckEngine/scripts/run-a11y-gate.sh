#!/usr/bin/env bash
# Accessibility rehearsal gate for Check Engine storefront surfaces (G6 / NFR-046).
# Scans Check Engine widgets only (.ce-root, [data-ce-theme]) and fails on serious/critical axe findings.
# Requires Node.js. Playwright + axe-core are installed into a disposable workdir on first run.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/../.." && pwd)"
cd "$ROOT_DIR"

if [[ -s "${NVM_DIR:-$HOME/.nvm}/nvm.sh" ]]; then
  # shellcheck disable=SC1091
  . "${NVM_DIR:-$HOME/.nvm}/nvm.sh"
fi

export CHECKENGINE_A11Y_BASE_URL="${CHECKENGINE_A11Y_BASE_URL:-${CHECKENGINE_CWV_BASE_URL:-http://127.0.0.1:5000}}"
export CHECKENGINE_A11Y_OUTPUT_DIR="${CHECKENGINE_A11Y_OUTPUT_DIR:-/tmp/checkengine-a11y}"
export CHECKENGINE_A11Y_WORKDIR="${CHECKENGINE_A11Y_WORKDIR:-/tmp/checkengine-a11y-npm}"
export CHECKENGINE_A11Y_PAGES="${CHECKENGINE_A11Y_PAGES:-/,/search?q=filter,/computers,/en/gmaster-bmw-parts-5,/build-your-own-computer,/en/gmaster-gm-11127548196-2,/ar/}"

if [[ -z "${CHECKENGINE_A11Y_CHROME:-}" && -z "${PLAYWRIGHT_BROWSER_PATH:-}" ]]; then
  if command -v google-chrome >/dev/null 2>&1; then
    export CHECKENGINE_A11Y_CHROME="$(command -v google-chrome)"
  elif command -v chrome >/dev/null 2>&1; then
    export CHECKENGINE_A11Y_CHROME="$(command -v chrome)"
  fi
fi

if ! command -v node >/dev/null 2>&1; then
  echo "[a11y-gate] node is required. Install Node.js 18+ or load nvm." >&2
  exit 2
fi

mkdir -p "$CHECKENGINE_A11Y_OUTPUT_DIR" "$CHECKENGINE_A11Y_WORKDIR"
if [[ ! -d "$CHECKENGINE_A11Y_WORKDIR/node_modules/playwright-core" || ! -d "$CHECKENGINE_A11Y_WORKDIR/node_modules/axe-core" ]]; then
  echo "[a11y-gate] installing playwright-core and axe-core into ${CHECKENGINE_A11Y_WORKDIR}"
  (cd "$CHECKENGINE_A11Y_WORKDIR" && npm init -y >/dev/null 2>&1 && npm install --no-fund --no-audit playwright-core@1.55.0 axe-core@4.10.3 >/dev/null)
fi

echo "[a11y-gate] scanning ${CHECKENGINE_A11Y_BASE_URL} pages=${CHECKENGINE_A11Y_PAGES}"
node "$SCRIPT_DIR/a11y-gate.mjs"
