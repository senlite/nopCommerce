#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SCALE="${1:-1.0}"
REPLACE="${2:-false}"

echo "Building Check Engine..."
bash "$ROOT/CheckEngine/scripts/build-checkengine.sh"

echo
echo "Reference-scale catalog load is an authenticated admin operation."
echo "POST Admin/CheckEngine/ReferenceDataAdmin/Load with JSON body:"
echo "{ \"scaleFactor\": $SCALE, \"replaceExisting\": $REPLACE, \"ensureBmwVehicleSeed\": true }"
echo
echo "Status:  GET  Admin/CheckEngine/ReferenceDataAdmin/Status"
echo "Purge:   POST Admin/CheckEngine/ReferenceDataAdmin/Purge"
echo
echo "Tagged synthetic rows use prefix '${SCALE}' scale and provenance ref-scale:v1 for rollback."
