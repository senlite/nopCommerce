# Check Engine demo on Oracle Cloud staging

Isolated Podman stack on the **same VM** as Senlite and Twin Particles ERP. No shared networks or volumes.

| | Value |
|---|--------|
| **Public URL** | https://checkengine.senlite.net/ |
| **OCI A record** | `checkengine` → `193.123.90.97` (HostGator `senlite.net` zone) |
| **Repo on VM** | `/opt/checkengine/senlite-commerce` |
| **Loopback** | `127.0.0.1:8081` → `checkengine_web` |
| **Database** | PostgreSQL 16 (`checkengine_db`, not published on the host) |

## Coexistence

| Stack | Containers | Ports |
|-------|------------|--------|
| Senlite marketplace/clinic | `abuzahra_*` | `:8000`, SPA `:8080` |
| Twin Particles company ERP | `twinparticles_*` | `:8001` |
| **Check Engine demo** | `checkengine_*` | **`:8081`** |

## First boot

SSH as `ubuntu` (or the linger-enabled Podman user).

```bash
sudo mkdir -p /opt/checkengine
sudo chown "$USER:$USER" /opt/checkengine
cd /opt/checkengine
git clone <this-repo-url> senlite-commerce
cd senlite-commerce/podman
cp env.example .env
nano .env   # set DB_PASSWORD

sed -i 's/\r$//' *.sh
chmod +x bootstrap.sh deploy.sh smoke.sh install-caddy-checkengine.sh
./bootstrap.sh
```

Open **http://127.0.0.1:8081/** (or SSH tunnel). Install wizard:

| Field | Value |
|-------|--------|
| Data provider | PostgreSQL |
| Server | `checkengine_db` |
| Database | `nopCommerce` |
| Username | `nopCommerce` |
| Password | same as `DB_PASSWORD` in `.env` |
| **Create sample data** | **checked** |
| Admin email / password | your staging credentials (do not reuse E2E `Admin123$`) |

Then Admin → Configuration → Local plugins → install **Check Engine**. That loads Check Engine product/vehicle demo seed (not the 2M-row reference-scale corpus).

## Public HTTPS

DNS: `A` `checkengine` → `193.123.90.97`.

```bash
cd /opt/checkengine/senlite-commerce/podman
./install-caddy-checkengine.sh
```

Caddy already terminates 80/443 for Senlite; this only appends the Check Engine site block.

## Later deploys

```bash
cd /opt/checkengine/senlite-commerce/podman
./deploy.sh
```

Rollback: `podman rm -f checkengine_web checkengine_db` and optionally `podman volume rm` the `checkengine_*` volumes. Leave Senlite and Twin Particles untouched.
