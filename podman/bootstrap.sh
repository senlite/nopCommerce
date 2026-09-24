#!/usr/bin/env bash
# First boot: build the production web image, start Postgres + nopCommerce.
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"

if [[ ! -f "$HERE/.env" ]]; then
  echo "Copy podman/env.example to podman/.env and set DB_PASSWORD." >&2
  exit 1
fi

# shellcheck disable=SC1091
set -a
source "$HERE/.env"
set +a

sed -i 's/\r$//' "$HERE"/*.sh 2>/dev/null || true
chmod +x "$HERE/bootstrap.sh" "$HERE/deploy.sh" "$HERE/smoke.sh" "$HERE/install-caddy-checkengine.sh" || true

compose() {
  export DOCKER_HOST="${DOCKER_HOST:-unix:///run/user/$(id -u)/podman/podman.sock}"
  if command -v docker-compose >/dev/null 2>&1; then
    docker-compose -f "$HERE/compose.yml" --env-file "$HERE/.env" "$@"
  elif podman compose version >/dev/null 2>&1; then
    podman compose -f "$HERE/compose.yml" --env-file "$HERE/.env" "$@"
  else
    podman-compose -f "$HERE/compose.yml" --env-file "$HERE/.env" "$@"
  fi
}

echo "==> Start Postgres"
compose up -d db

echo "==> Build and start Check Engine web"
compose up -d --build web

WEB_PORT="${WEB_PORT:-8081}"
echo "Bootstrap complete."
echo "  Store: http://127.0.0.1:${WEB_PORT}/"
echo "  Postgres container: checkengine_db  host from web: checkengine_db  port 5432"
echo "  Install wizard: PostgreSQL, Install sample data = ON, then Admin → Local plugins → install Check Engine."
echo "  Public host after Caddy: https://${PUBLIC_HOST:-checkengine.senlite.net}/"
