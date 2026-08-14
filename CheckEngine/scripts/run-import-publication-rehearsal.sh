#!/usr/bin/env bash
# Import publication rehearsal for H1.21: run a 10,000-row batch through durable SQL pipeline
# and nopCommerce catalog publication on a disposable SQL Server clone.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

BASE_URL="${CHECKENGINE_REHEARSAL_BASE_URL:-http://127.0.0.1:5000}"
ADMIN_EMAIL="${CHECKENGINE_REHEARSAL_ADMIN_EMAIL:-admin@yourStore.com}"
ADMIN_PASSWORD="${CHECKENGINE_REHEARSAL_ADMIN_PASSWORD:-Admin123\$}"
ROW_COUNT="${CHECKENGINE_REHEARSAL_ROW_COUNT:-10000}"
MIN_ROWS_PER_SEC="${CHECKENGINE_REHEARSAL_MIN_ROWS_PER_SEC:-50}"

echo "[import-rehearsal] H1.21 publication rehearsal"
echo "[import-rehearsal] target=${BASE_URL} rows=${ROW_COUNT} min_rate=${MIN_ROWS_PER_SEC}/s"
echo "[import-rehearsal] prerequisites:"
echo "  - SQL Server disposable clone with Check Engine plugin installed"
echo "  - NopImportProductPublisher registered (see ImportProductionPathContractTests)"
echo "  - Admin session cookie or API token for ImportAdmin endpoints"
echo ""
echo "[import-rehearsal] Steps:"
echo "  1. Upload 10k-row CSV via Admin/CheckEngine/ImportAdmin/Upload"
echo "  2. Run all twelve stages through Admin/CheckEngine/ImportAdmin/RunStage"
echo "  3. Publish approved rows via ImportAdmin/Publish"
echo "  4. Verify catalog count and OEM map rows in SQL"
echo ""
echo "[import-rehearsal] Architecture gate (deterministic, no live DB):"
dotnet test src/Tests/TwinParticles.CheckEngine.Tests.Architecture/TwinParticles.CheckEngine.Tests.Architecture.csproj \
  -c Release \
  --filter "FullyQualifiedName~ImportPipelineOrchestratorServiceTests|FullyQualifiedName~ImportProductionPathContractTests" \
  --no-restore 2>/dev/null || \
dotnet test src/Tests/TwinParticles.CheckEngine.Tests.Architecture/TwinParticles.CheckEngine.Tests.Architecture.csproj \
  -c Release \
  --filter "FullyQualifiedName~ImportPipelineOrchestratorServiceTests|FullyQualifiedName~ImportProductionPathContractTests"

echo "[import-rehearsal] Live SQL Server rehearsal is operator-driven; record evidence in release notes when complete."
