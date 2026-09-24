#!/usr/bin/env bash
# Install HTTPS for checkengine.senlite.net → Podman :8081 (host Caddy + Let's Encrypt).
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
SNIPPET="$HERE/Caddyfile.example"
CADDYFILE=/etc/caddy/Caddyfile

if ! command -v caddy >/dev/null 2>&1; then
  echo "Install Caddy first (Senlite staging: Abuzahraptc dev/podman/install-caddy.sh)." >&2
  exit 1
fi

if [[ ! -f "$SNIPPET" ]]; then
  echo "Missing $SNIPPET" >&2
  exit 1
fi

sudo python3 - <<PY
from pathlib import Path
caddy = Path("$CADDYFILE")
snippet = Path("$SNIPPET").read_text()
# Keep only the site block (drop comment header if present).
start = snippet.find("checkengine.senlite.net")
if start == -1:
    raise SystemExit("Caddyfile.example missing checkengine.senlite.net")
snippet = snippet[start:]
text = caddy.read_text() if caddy.exists() else ""
marker = "checkengine.senlite.net"
if marker in text:
    start = text.find(marker)
    start = text.rfind("\n", 0, start)
    if start < 0:
        start = 0
    end = text.find("\n\n", start)
    end = len(text) if end == -1 else end + 1
    text = text[:start] + "\n" + snippet.rstrip() + "\n" + text[end:]
else:
    text = text.rstrip() + "\n\n" + snippet.rstrip() + "\n"
caddy.write_text(text)
print("Caddyfile updated")
PY

sudo caddy validate --config "$CADDYFILE"
sudo systemctl reload caddy || sudo systemctl restart caddy
echo "TLS ok: https://checkengine.senlite.net/ → 127.0.0.1:8081"
