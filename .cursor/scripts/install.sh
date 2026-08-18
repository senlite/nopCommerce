#!/usr/bin/env bash
# Idempotent repository bootstrap for cloud agents.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

echo "[cloud-agent install] Ensuring rclone is available..."
if ! command -v rclone >/dev/null 2>&1; then
  curl -fsSL https://rclone.org/install.sh | sudo bash
fi
rclone version | head -1

if command -v dotnet >/dev/null 2>&1; then
  echo "[cloud-agent install] Restoring Check Engine plugin (fast path)..."
  dotnet restore src/Plugins/TwinParticles.CheckEngine/TwinParticles.CheckEngine.csproj >/dev/null 2>&1 || true
fi

mkdir -p /opt/cursor/artifacts 2>/dev/null || true
chmod +x "$ROOT/scripts/upload-artifacts-to-onedrive.sh" 2>/dev/null || true
chmod +x "$ROOT/scripts/split-rclone-config-for-cursor.sh" 2>/dev/null || true
chmod +x "$ROOT/.cursor/scripts/rclone-config-from-secrets.sh" 2>/dev/null || true

echo "[cloud-agent install] Done."
