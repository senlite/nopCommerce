#!/usr/bin/env bash
# Per-boot setup: materialize rclone config from environment secrets.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=rclone-config-from-secrets.sh
source "${SCRIPT_DIR}/rclone-config-from-secrets.sh"

RCLONE_DIR="${HOME}/.config/rclone"
mkdir -p "$RCLONE_DIR"

if write_rclone_config_from_secrets "${RCLONE_DIR}/rclone.conf"; then
  if [[ -n "${RCLONE_CONFIG_B64_2:-}" ]]; then
    echo "[cloud-agent start] Wrote rclone config from split RCLONE_CONFIG_B64 secrets."
  else
    echo "[cloud-agent start] Wrote rclone config from RCLONE_CONFIG_B64."
  fi
elif [[ -n "${RCLONE_CONFIG_B64:-}" || -n "${RCLONE_CONFIG_B64_2:-}" ]]; then
  echo "[cloud-agent start] RCLONE_CONFIG_B64 secret(s) are not valid base64; OneDrive upload disabled." >&2
elif [[ ! -f "${RCLONE_DIR}/rclone.conf" ]]; then
  echo "[cloud-agent start] OneDrive upload not configured (missing RCLONE_CONFIG_B64 secret)." >&2
fi

if [[ -f "${RCLONE_DIR}/rclone.conf" ]] && command -v rclone >/dev/null 2>&1; then
  # Failures here are non-fatal; upload script will surface auth errors.
  rclone listremotes >/dev/null 2>&1 && echo "[cloud-agent start] rclone remotes: $(rclone listremotes | tr -d '\n')"
fi

echo "[cloud-agent start] Done."
