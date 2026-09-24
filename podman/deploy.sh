#!/usr/bin/env bash
# Rebuild and restart the Check Engine web container. Postgres volume is kept.
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
# shellcheck disable=SC1091
source "$HERE/.env"

export DOCKER_HOST="${DOCKER_HOST:-unix:///run/user/$(id -u)/podman/podman.sock}"

compose() {
  if command -v docker-compose >/dev/null 2>&1; then
    docker-compose -f "$HERE/compose.yml" --env-file "$HERE/.env" "$@"
  elif podman compose version >/dev/null 2>&1; then
    podman compose -f "$HERE/compose.yml" --env-file "$HERE/.env" "$@"
  else
    podman-compose -f "$HERE/compose.yml" --env-file "$HERE/.env" "$@"
  fi
}

if [[ -d "$HERE/../.git" ]]; then
  git -C "$HERE/.." pull --ff-only || true
fi

sed -i 's/\r$//' "$HERE"/*.sh 2>/dev/null || true

compose up -d --build web
compose up -d db

if [[ -x "$HERE/smoke.sh" ]]; then
  "$HERE/smoke.sh"
fi

echo "Deploy complete. checkengine_web rebuilt; checkengine_db volume unchanged."
