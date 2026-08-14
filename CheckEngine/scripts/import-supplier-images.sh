#!/usr/bin/env bash
# Operator helper for H1.24 supplier-image manifest sourcing (CSV sku,url[,seoName]).
set -euo pipefail

if [[ $# -lt 2 ]]; then
  echo "Usage: $0 <base-url> <manifest.csv>" >&2
  exit 1
fi

BASE_URL="${1%/}"
MANIFEST="$2"
ADMIN_COOKIE="${CHECKENGINE_ADMIN_COOKIE:-}"

if [[ ! -f "$MANIFEST" ]]; then
  echo "Manifest not found: $MANIFEST" >&2
  exit 1
fi

CSV_CONTENT="$(python3 - <<'PY' "$MANIFEST"
import json, sys
print(json.dumps(open(sys.argv[1], encoding='utf-8').read()))
PY
)"

curl -sS -X POST "${BASE_URL}/Admin/CheckEngine/ImageAdmin/SourceFromManifest" \
  -H "Content-Type: application/json" \
  ${ADMIN_COOKIE:+-H "Cookie: ${ADMIN_COOKIE}"} \
  --data-binary "{\"csvContent\":${CSV_CONTENT}}"

echo
