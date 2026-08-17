#!/usr/bin/env bash
# Per-boot setup: materialize rclone config from environment secrets.
set -euo pipefail

RCLONE_DIR="${HOME}/.config/rclone"
mkdir -p "$RCLONE_DIR"

if [[ -n "${RCLONE_CONFIG_B64:-}" ]]; then
  # Secret is injected by Cursor environment settings (never commit this value).
  # Invalid secret must not fail environment start — uploads will surface the error.
  if printf '%s' "$RCLONE_CONFIG_B64" | base64 -d > "$RCLONE_DIR/rclone.conf" 2>/dev/null; then
    chmod 600 "$RCLONE_DIR/rclone.conf"
    echo "[cloud-agent start] Wrote rclone config from RCLONE_CONFIG_B64."
  else
    rm -f "$RCLONE_DIR/rclone.conf"
    echo "[cloud-agent start] RCLONE_CONFIG_B64 is not valid base64; OneDrive upload disabled." >&2
  fi
elif [[ ! -f "$RCLONE_DIR/rclone.conf" ]]; then
  echo "[cloud-agent start] OneDrive upload not configured (missing RCLONE_CONFIG_B64 secret)." >&2
fi

if [[ -f "$RCLONE_DIR/rclone.conf" ]] && command -v rclone >/dev/null 2>&1; then
  # Failures here are non-fatal; upload script will surface auth errors.
  rclone listremotes >/dev/null 2>&1 && echo "[cloud-agent start] rclone remotes: $(rclone listremotes | tr -d '\n')"
fi

echo "[cloud-agent start] Done."
