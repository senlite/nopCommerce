#!/usr/bin/env bash
# Deterministic local build + architecture test gate for TwinParticles.CheckEngine.
# Do NOT use NopCommerce.sln — it is broken for this plugin workflow.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

CONFIGURATION="${CONFIGURATION:-Release}"

PLUGIN_CSPROJ="src/Plugins/TwinParticles.CheckEngine/TwinParticles.CheckEngine.csproj"
ARCH_CSPROJ="src/Tests/TwinParticles.CheckEngine.Tests.Architecture/TwinParticles.CheckEngine.Tests.Architecture.csproj"
E2E_CSPROJ="src/Tests/TwinParticles.CheckEngine.Tests.E2E/TwinParticles.CheckEngine.Tests.E2E.csproj"

echo "[checkengine-build] step 1/4: restore + build plugin"
dotnet restore "$PLUGIN_CSPROJ"
dotnet build "$PLUGIN_CSPROJ" -c "$CONFIGURATION" --no-restore

echo "[checkengine-build] step 2/4: restore + build architecture tests"
dotnet restore "$ARCH_CSPROJ"
dotnet build "$ARCH_CSPROJ" -c "$CONFIGURATION" --no-restore

if [[ -f "$E2E_CSPROJ" ]]; then
  echo "[checkengine-build] step 3/4: restore + build E2E tests"
  dotnet restore "$E2E_CSPROJ"
  dotnet build "$E2E_CSPROJ" -c "$CONFIGURATION" --no-restore
else
  echo "[checkengine-build] step 3/4: E2E project not found — skipped"
fi

echo "[checkengine-build] step 4/4: run architecture tests ($CONFIGURATION)"
dotnet test "$ARCH_CSPROJ" -c "$CONFIGURATION" --no-build

echo "[checkengine-build] done"
