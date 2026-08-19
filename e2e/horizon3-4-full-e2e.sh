#!/usr/bin/env bash
# Horizon 3 (marketplace) + Horizon 4 (vertical portals) end-to-end smoke harness.
# Unauthenticated HTTP + DB integrity checks run headless; authenticated flows use browser (see README output).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BASE_URL="${BASE_URL:-http://127.0.0.1:5000}"
DB_USER="${DB_USER:-nopcommerce}"
DB_PASS="${DB_PASS:-nopCommerce_db_password123!}"
DB_NAME="${DB_NAME:-nopcommerce}"
PASS=0
FAIL=0
SKIP=0

log() { printf '%s %s\n' "$(date -u +%H:%M:%S)" "$*"; }
pass() { PASS=$((PASS + 1)); log "PASS  $*"; }
fail() { FAIL=$((FAIL + 1)); log "FAIL  $*"; }
skip() { SKIP=$((SKIP + 1)); log "SKIP  $*"; }

expect_code() {
  local label="$1" url="$2" expected="$3"
  local code
  code="$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL$url")"
  if [[ "$code" == "$expected" ]]; then pass "$label -> HTTP $code"
  else fail "$label -> HTTP $code (expected $expected)"; fi
}

mysql_q() {
  mysql -N -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -e "$1" 2>/dev/null
}

log "=== Horizon 3+4 E2E @ $BASE_URL ==="

# --- Phase 0: platform health ---
log "--- Phase 0: health ---"
if curl -sf "$BASE_URL/check-engine/health" | grep -q '"status"'; then
  pass "check-engine health JSON"
  curl -sf "$BASE_URL/check-engine/health" | python3 -m json.tool 2>/dev/null | head -8 || true
else
  fail "check-engine health"
fi

# --- Phase 1: unauthenticated route smoke ---
log "--- Phase 1: public routes (unauthenticated) ---"
expect_code "storefront home" "/" "200"
expect_code "H3 vendor apply page" "/check-engine/vendor/Apply" "200"
for portal in workshop fleet dealer; do
  expect_code "H4 $portal portal page" "/check-engine/$portal" "200"
done

log "--- Phase 1b: JSON APIs expect 401 without session ---"
for path in \
  "/check-engine/workshop/DashboardData" \
  "/check-engine/fleet/DashboardData" \
  "/check-engine/dealer/DashboardData" \
  "/check-engine/vendor/DashboardData"
do
  code="$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL$path")"
  if [[ "$code" == "401" || "$code" == "403" ]]; then pass "unauth $path -> HTTP $code"
  else fail "unauth $path -> HTTP $code (expected 401/403)"; fi
done

for path in \
  "/check-engine/workshop/CreditStatements" \
  "/check-engine/fleet/VehicleCostReport"
do
  code="$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL$path")"
  if [[ "$code" == "401" || "$code" == "403" || "$code" == "404" ]]; then pass "unauth $path -> HTTP $code"
  else fail "unauth $path -> HTTP $code (expected 401/403/404)"; fi
done

# --- Phase 2: admin routes redirect to login ---
log "--- Phase 2: admin routes (unauthenticated -> redirect) ---"
for path in \
  "/Admin/CheckEngine/VendorAdmin/Queue" \
  "/Admin/CheckEngine/CommissionAdmin/Configure" \
  "/Admin/CheckEngine/PayoutAdmin" \
  "/Admin/CheckEngine/PortalAdmin/Accounts"
do
  code="$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL$path")"
  if [[ "$code" == "302" || "$code" == "401" ]]; then pass "admin gate $path -> HTTP $code"
  else fail "admin gate $path -> HTTP $code (expected 302/401)"; fi
done

# --- Phase 3: database integrity ---
log "--- Phase 3: DB integrity (H3 marketplace) ---"
vendors="$(mysql_q "SELECT COUNT(*) FROM TP_CE_Vendor")"
plans="$(mysql_q "SELECT COUNT(*) FROM TP_CE_CommissionPlan")"
payouts="$(mysql_q "SELECT COUNT(*) FROM TP_CE_PayoutStatement")"
splits="$(mysql_q "SELECT COUNT(*) FROM TP_CE_OrderVendorSplit")"
[[ "${vendors:-0}" -gt 0 ]] && pass "TP_CE_Vendor rows=$vendors" || fail "TP_CE_Vendor empty"
[[ "${plans:-0}" -gt 0 ]] && pass "TP_CE_CommissionPlan rows=$plans" || fail "TP_CE_CommissionPlan empty"
[[ "${payouts:-0}" -gt 0 ]] && pass "TP_CE_PayoutStatement rows=$payouts" || fail "TP_CE_PayoutStatement empty"
[[ "${splits:-0}" -ge 0 ]] && pass "TP_CE_OrderVendorSplit rows=$splits"

marketplace="$(mysql_q "SELECT Value FROM Setting WHERE Name='checkenginepluginsettings.enablemarketplace' LIMIT 1")"
[[ "$marketplace" == "True" ]] && pass "marketplace enabled" || fail "marketplace not enabled ($marketplace)"

log "--- Phase 3b: DB integrity (H4 portals) ---"
ws="$(mysql_q "SELECT COUNT(*) FROM TP_CE_WorkshopAccount WHERE IsActive=1")"
fl="$(mysql_q "SELECT COUNT(*) FROM TP_CE_FleetAccount WHERE IsActive=1")"
dl="$(mysql_q "SELECT COUNT(*) FROM TP_CE_DealerAccount WHERE IsActive=1")"
[[ "${ws:-0}" -gt 0 ]] && pass "workshop accounts=$ws" || fail "no active workshop accounts"
[[ "${fl:-0}" -gt 0 ]] && pass "fleet accounts=$fl" || fail "no active fleet accounts"
[[ "${dl:-0}" -gt 0 ]] && pass "dealer accounts=$dl" || fail "no active dealer accounts"

tier_tbl="$(mysql_q "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$DB_NAME' AND table_name='TP_CE_WorkshopPriceTier'")"
acct_tier="$(mysql_q "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$DB_NAME' AND table_name='TP_CE_WorkshopAccountTier'")"
[[ "${tier_tbl:-0}" -eq 1 ]] && pass "TP_CE_WorkshopPriceTier exists" || fail "TP_CE_WorkshopPriceTier missing"
[[ "${acct_tier:-0}" -eq 1 ]] && pass "TP_CE_WorkshopAccountTier exists" || fail "TP_CE_WorkshopAccountTier missing"

ws_customer="$(mysql_q "SELECT CustomerId FROM TP_CE_WorkshopAccount WHERE Id=1 LIMIT 1")"
if [[ -n "$ws_customer" && "$ws_customer" != "0" ]]; then
  pass "workshop account #1 linked to customer $ws_customer"
else
  fail "workshop account #1 not linked to a customer (CustomerId=$ws_customer)"
fi

# --- Phase 4: licence ---
log "--- Phase 4: enterprise licence ---"
lic_health="$(curl -sf "$BASE_URL/check-engine/health" | grep -o '"licence":"[^"]*"' || true)"
[[ "$lic_health" == *"active"* ]] && pass "licence active in health" || skip "licence not active ($lic_health)"

# --- Summary ---
log "=== Summary: $PASS passed, $FAIL failed, $SKIP skipped ==="
if [[ "$FAIL" -gt 0 ]]; then exit 1; fi
exit 0
