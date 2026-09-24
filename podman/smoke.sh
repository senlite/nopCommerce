#!/usr/bin/env bash
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
if [[ -f "$HERE/.env" ]]; then
  # shellcheck disable=SC1091
  set -a
  source "$HERE/.env"
  set +a
fi

URL="http://127.0.0.1:${WEB_PORT:-8081}/"

echo "Smoke: GET $URL"
for _ in $(seq 1 30); do
  if curl -fsS -o /dev/null -w "%{http_code}" "$URL" | grep -Eq '^(200|302)$'; then
    echo "ok"
    exit 0
  fi
  sleep 2
done

echo "Smoke failed: $URL did not return 200/302" >&2
exit 1
