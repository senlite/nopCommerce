#!/usr/bin/env bash
# Generate a signed Enterprise ce-lic-v1 bundle for local portal E2E (fleet + dealer).
# Uses the store EncryptionKey (same secret HmacLicenceKeyValidator uses).
set -euo pipefail

SIGNING_KEY="${1:-}"
if [[ -z "$SIGNING_KEY" ]]; then
  SIGNING_KEY="$(mysql -N -u nopcommerce -p'nopCommerce_db_password123!' nopcommerce \
    -e "SELECT Value FROM Setting WHERE Name='securitysettings.encryptionkey' LIMIT 1" 2>/dev/null || true)"
fi

if [[ -z "$SIGNING_KEY" ]]; then
  echo "Usage: $0 [signing-key]" >&2
  echo "Or ensure MySQL nopcommerce DB is reachable with default dev credentials." >&2
  exit 1
fi

export SIGNING_KEY
python3 << 'PY'
import base64, hashlib, hmac, json, os
from datetime import datetime, timedelta, timezone

key = os.environ["SIGNING_KEY"].encode("utf-8")
exp = (datetime.now(timezone.utc) + timedelta(days=365)).strftime("%Y-%m-%dT%H:%M:%S.0000000+00:00")
payload = {
    "exp": exp,
    "tier": "Enterprise",
    "workshop": True,
    "fleet": True,
    "dealer": True,
}
raw = json.dumps(payload, separators=(",", ":")).encode("utf-8")
segment = base64.b64encode(raw).decode("ascii")
sig = hmac.new(key, raw, hashlib.sha256).hexdigest().upper()
print(f"ce-lic-v1.{segment}.{sig}")
PY
