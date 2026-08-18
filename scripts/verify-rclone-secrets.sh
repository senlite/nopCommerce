#!/usr/bin/env bash
# Check whether RCLONE_CONFIG_B64 secret(s) decode to a plausible onedrive rclone.conf.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=../.cursor/scripts/rclone-config-from-secrets.sh
source "${SCRIPT_DIR}/../.cursor/scripts/rclone-config-from-secrets.sh"

if [[ -z "${RCLONE_CONFIG_B64:-}" ]]; then
  echo "FAIL: RCLONE_CONFIG_B64 is not set (start a new agent after saving secrets)."
  exit 1
fi

echo "RCLONE_CONFIG_B64 length: ${#RCLONE_CONFIG_B64}"
if [[ -n "${RCLONE_CONFIG_B64_2:-}" ]]; then
  echo "RCLONE_CONFIG_B64_2 length: ${#RCLONE_CONFIG_B64_2}"
fi

b64="$(assemble_rclone_config_b64)"
b64="${b64//$'\n'/}"; b64="${b64//$'\r'/}"; b64="${b64// /}"
echo "Combined base64 length: ${#b64}"

if [[ "${RCLONE_CONFIG_B64:0:20}" == "Ima/kJx5wzkRUAUVR3Pv" ]]; then
  echo "FAIL: Secrets match the agent test/example chunks, not a real rclone.conf."
  echo "Run scripts/split-rclone-config-for-cursor.sh on your PC and replace both secrets."
  exit 1
fi

if ! decoded="$(printf '%s' "$b64" | base64 -d 2>/dev/null)"; then
  echo "FAIL: Combined value is not valid base64."
  exit 1
fi

first_line="$(printf '%s' "$decoded" | head -1)"
echo "First decoded line: ${first_line}"

if [[ ! "$first_line" =~ ^\[.+\]$ ]]; then
  echo "FAIL: First line should be [remote_name] (e.g. [onedrive]). Wrong split or wrong file encoded."
  exit 1
fi

if ! printf '%s' "$decoded" | grep -qE 'type[[:space:]]*=[[:space:]]*onedrive'; then
  echo "WARN: No 'type = onedrive' found — remote name in config may differ from 'onedrive:'."
fi

if write_rclone_config_from_secrets "${HOME}/.config/rclone/rclone.conf"; then
  echo "OK: Wrote ~/.config/rclone/rclone.conf"
  if command -v rclone >/dev/null 2>&1; then
    rclone listremotes
  fi
  exit 0
fi

echo "FAIL: Could not write valid rclone config."
exit 1
