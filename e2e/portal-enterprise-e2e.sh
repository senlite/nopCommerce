#!/usr/bin/env bash
# Print a signed Enterprise ce-lic-v1 key and verify portal routes (fleet/dealer need Enterprise).
# Activate via Admin → Check Engine → Configure → Licence panel, or paste into Dashboard panel.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BASE_URL="${BASE_URL:-http://127.0.0.1:5000}"

LICENCE="$("$ROOT/e2e/generate-enterprise-licence.sh")"
echo "Enterprise licence key (paste into Configure → Licence & entitlements):"
echo "$LICENCE"
echo ""

for portal in workshop fleet dealer; do
  code="$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/check-engine/$portal")"
  echo "$portal page (unauthenticated): HTTP $code"
done

echo ""
echo "After activating in admin UI, provision accounts at:"
echo "  $BASE_URL/Admin/CheckEngine/PortalAdmin/Accounts"
echo ""
echo "Configure page with licence panel:"
echo "  $BASE_URL/Admin/CheckEngine/Configure"
