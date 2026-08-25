#!/usr/bin/env bash
# Pack TwinParticles.CheckEngine.{version}.zip (G11 commercial packaging rehearsal).
# Does not sign. Production licence vendor signing remains an external gate.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/../.." && pwd)"
cd "$ROOT_DIR"

export CHECKENGINE_PACK_OUT="${CHECKENGINE_PACK_OUT:-/tmp/checkengine-pack}"
CONFIGURATION="${CONFIGURATION:-Release}"

BUILD_FLAG=()
if [[ "${CHECKENGINE_PACK_BUILD:-1}" == "1" ]]; then
  BUILD_FLAG=(--build)
fi

echo "[pack-gate] out=${CHECKENGINE_PACK_OUT} configuration=${CONFIGURATION}"
python3 "$SCRIPT_DIR/pack-checkengine.py" --configuration "$CONFIGURATION" --out-dir "$CHECKENGINE_PACK_OUT" "${BUILD_FLAG[@]}"
