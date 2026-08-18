#!/usr/bin/env bash
# Assemble base64 rclone.conf from Cursor secrets (single or split across 4096-char limit).
# Part 1: RCLONE_CONFIG_B64, part 2+: RCLONE_CONFIG_B64_2, RCLONE_CONFIG_B64_3, ...
set -euo pipefail

assemble_rclone_config_b64() {
  local combined="" suffix="" var value
  while true; do
    if [[ -z "$suffix" ]]; then
      var="RCLONE_CONFIG_B64"
    else
      var="RCLONE_CONFIG_B64${suffix}"
    fi
    value="${!var:-}"
    if [[ -z "$value" ]]; then
      break
    fi
    combined+="$value"
    if [[ -z "$suffix" ]]; then
      suffix="_2"
    else
      suffix="_$(( ${suffix#_} + 1 ))"
    fi
  done
  if [[ -z "$combined" ]]; then
    return 1
  fi
  printf '%s' "$combined"
}

write_rclone_config_from_secrets() {
  local target="${1:-${HOME}/.config/rclone/rclone.conf}"
  local b64 dir decoded

  if ! b64="$(assemble_rclone_config_b64)"; then
    return 1
  fi

  # Strip accidental whitespace/newlines from dashboard paste.
  b64="${b64//$'\n'/}"
  b64="${b64//$'\r'/}"
  b64="${b64// /}"

  dir="$(dirname "$target")"
  mkdir -p "$dir"

  if ! decoded="$(printf '%s' "$b64" | base64 -d 2>/dev/null)"; then
    rm -f "$target"
    return 2
  fi

  # Valid rclone.conf is INI text starting with [remote_name].
  if [[ ! "$decoded" =~ ^\[.+\] ]] || [[ ! "$decoded" =~ type[[:space:]]*=[[:space:]]*onedrive ]]; then
    rm -f "$target"
    return 3
  fi

  printf '%s' "$decoded" > "$target"
  chmod 600 "$target"
  return 0
}

if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
  if write_rclone_config_from_secrets "${1:-}"; then
    echo "Wrote ${1:-${HOME}/.config/rclone/rclone.conf}"
  else
    echo "Failed to write rclone config from secrets." >&2
    exit 1
  fi
fi
