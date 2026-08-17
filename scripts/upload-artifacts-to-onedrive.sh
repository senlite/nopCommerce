#!/usr/bin/env bash
# Upload /opt/cursor/artifacts to OneDrive via rclone.
# Requires environment secret RCLONE_CONFIG_B64 (see docs/cloud-agent-onedrive-setup.md).
set -euo pipefail

ARTIFACTS_DIR="${ARTIFACTS_DIR:-/opt/cursor/artifacts}"
REMOTE="${ONEDRIVE_RCLONE_REMOTE:-onedrive:}"
FOLDER="${ONEDRIVE_ARTIFACTS_FOLDER:-CheckEngine/AgentArtifacts}"
RUN_ID="${CURSOR_CONVERSATION_ID:-${CURSOR_AGENT_RUN_ID:-manual-$(date +%Y%m%d-%H%M%S)}}"
DATE_PREFIX="$(date +%Y-%m-%d)"
DEST="${REMOTE%/}/${FOLDER}/${DATE_PREFIX}/${RUN_ID}"

if ! command -v rclone >/dev/null 2>&1; then
  echo "rclone is not installed. Rebuild the Cursor environment or run .cursor/scripts/install.sh" >&2
  exit 1
fi

if [[ ! -d "${ARTIFACTS_DIR}" ]]; then
  echo "Artifacts directory not found: ${ARTIFACTS_DIR}" >&2
  exit 1
fi

# Ensure rclone.conf exists (start.sh normally runs at boot)
if [[ ! -f "${HOME}/.config/rclone/rclone.conf" ]]; then
  if [[ -n "${RCLONE_CONFIG_B64:-}" ]]; then
    bash "$(dirname "$0")/../.cursor/scripts/start.sh" 2>/dev/null || true
  fi
fi

if [[ ! -f "${HOME}/.config/rclone/rclone.conf" ]]; then
  cat >&2 <<'EOF'
OneDrive is not configured for this cloud agent environment.

Add secret RCLONE_CONFIG_B64 in Cursor → Cloud Agents → Environments → Secrets.
See docs/cloud-agent-onedrive-setup.md for how to generate it on your PC.
EOF
  exit 1
fi

shopt -s nullglob
files=("${ARTIFACTS_DIR}"/*)
if [[ ${#files[@]} -eq 0 ]]; then
  echo "No files in ${ARTIFACTS_DIR}; nothing to upload."
  exit 0
fi

echo "Uploading ${#files[@]} file(s) to ${DEST} ..."
rclone copy "${ARTIFACTS_DIR}" "${DEST}" --progress --stats-one-line

echo ""
echo "Uploaded to OneDrive path: ${DEST}"
echo "Share links (valid until you revoke access in OneDrive):"
for f in "${files[@]}"; do
  name="$(basename "$f")"
  if link="$(rclone link "${DEST}/${name}" 2>/dev/null)"; then
    echo "  ${name}: ${link}"
  else
    echo "  ${name}: (could not create link — open folder in OneDrive and share manually)"
  fi
done
