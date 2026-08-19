#!/usr/bin/env bash
# Split a base64-encoded rclone.conf into Cursor environment secret chunks.
# Cursor secret values are limited to 4096 characters; use CHUNK_SIZE=4000 for margin.
set -euo pipefail

CONF="${1:-${HOME}/.config/rclone/rclone.conf}"
CHUNK_SIZE=4000
CURSOR_MAX=4096

if [[ ! -f "$CONF" ]]; then
  echo "rclone config not found: $CONF" >&2
  exit 1
fi

if base64 -w0 /dev/null >/dev/null 2>&1; then
  B64="$(base64 -w0 "$CONF")"
else
  B64="$(base64 -i "$CONF" | tr -d '\n')"
fi

LEN="${#B64}"
echo "Base64 length: ${LEN} (Cursor max per secret: ${CURSOR_MAX})"
echo ""

if [[ "$LEN" -le "$CURSOR_MAX" ]]; then
  echo "Fits in one secret. Add as:"
  echo "  Name:  RCLONE_CONFIG_B64"
  echo "  Value: (${LEN} chars — paste the line below)"
  echo ""
  echo "$B64"
  exit 0
fi

PART=1
PARTS=0
POS=0
while [[ "$POS" -lt "$LEN" ]]; do
  CHUNK="${B64:POS:CHUNK_SIZE}"
  CHUNK_LEN="${#CHUNK}"
  if [[ "$PART" -eq 1 ]]; then
    SECRET_NAME="RCLONE_CONFIG_B64"
  else
    SECRET_NAME="RCLONE_CONFIG_B64_${PART}"
  fi

  echo "=== Secret: ${SECRET_NAME} (${CHUNK_LEN} chars) ==="
  echo "$CHUNK"
  echo ""

  POS=$((POS + CHUNK_SIZE))
  PART=$((PART + 1))
  PARTS=$((PARTS + 1))
done

echo "Add all ${PARTS} secret(s) above in Cursor → Cloud Agents → Environments → Secrets."
echo "Agents concatenate them in order at boot (see docs/cloud-agent-onedrive-setup.md)."
