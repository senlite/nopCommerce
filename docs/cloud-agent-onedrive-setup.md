# Cloud agent → OneDrive artifact uploads

Screen recordings and screenshots saved under `/opt/cursor/artifacts/` can be uploaded automatically to your OneDrive using **rclone**.

## One-time setup (on your PC)

### 1. Install rclone

- Windows: [rclone downloads](https://rclone.org/downloads/) or `winget install Rclone.Rclone`
- macOS: `brew install rclone`

### 2. Authorize OneDrive

```bash
rclone config create onedrive onedrive
# Follow the browser OAuth flow when prompted
```

Or interactively:

```bash
rclone config
# n) New remote → name: onedrive → type: onedrive → follow prompts
```

### 3. Create the base64 secret

**Linux / macOS / Git Bash:**

```bash
base64 -w0 ~/.config/rclone/rclone.conf   # Linux
# macOS:
base64 -i ~/.config/rclone/rclone.conf | tr -d '\n'
```

**PowerShell (Windows):**

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("$env:APPDATA\rclone\rclone.conf"))
```

Copy the **single line** of output.

#### If Cursor rejects the secret (max 4096 characters)

OneDrive `rclone.conf` tokens often encode to **5000+** characters. Cursor environment secrets are capped at **4096** per value.

Run the splitter on your PC (from a clone of this repo, or copy the script):

```bash
bash scripts/split-rclone-config-for-cursor.sh
# or: bash scripts/split-rclone-config-for-cursor.sh /path/to/rclone.conf
```

It prints two or more chunks. Add **each** as a separate secret:

| Secret | When |
|--------|------|
| `RCLONE_CONFIG_B64` | Always — part 1 |
| `RCLONE_CONFIG_B64_2` | When part 1 alone exceeds 4096 chars |
| `RCLONE_CONFIG_B64_3` | Third chunk if needed |

Agents concatenate the parts in order at boot. A **5764**-character value needs **two** secrets (4000 + 1760 chars when using the splitter).

**Verify on your PC before saving secrets** — the first chunk must decode to `[onedrive]`:

```bash
B64=$(base64 -w0 ~/.config/rclone/rclone.conf)   # Linux
printf '%s' "${B64:0:4000}" | base64 -d | head -1
```

Do **not** paste example output from agent logs or documentation.

### 4. Add Cursor environment secrets

Open your environment in the dashboard:

**Cursor → Cloud Agents → Environments → (your environment) → Secrets**

| Secret | Required | Example |
|--------|----------|---------|
| `RCLONE_CONFIG_B64` | Yes | Part 1 of base64 (≤4096 chars) |
| `RCLONE_CONFIG_B64_2` | If split | Part 2 when total base64 > 4096 |
| `RCLONE_CONFIG_B64_3` | If split | Part 3 if still needed |
| `ONEDRIVE_ARTIFACTS_FOLDER` | No | `CheckEngine/AgentArtifacts` |
| `ONEDRIVE_RCLONE_REMOTE` | No | `onedrive:` (must match remote name in config) |

Do **not** commit a `.cursor/environment.json` with a custom Dockerfile. This repo uses a dashboard-managed (personal) environment; a committed Dockerfile would replace the existing agent image and drop .NET, Chrome, and VNC.

The environment **install** / **start** scripts should be:

```text
install: bash .cursor/scripts/install.sh
start:   bash .cursor/scripts/start.sh
```

After adding secrets, **Save** the environment so future agents receive `RCLONE_CONFIG_B64` at boot. This already-running agent will not pick up a newly added secret.

## Usage in a cloud agent run

After saving a recording or screenshot:

```bash
bash scripts/upload-artifacts-to-onedrive.sh
```

Files land at:

```text
OneDrive/CheckEngine/AgentArtifacts/YYYY-MM-DD/{conversation-id}/
```

The script prints **rclone link** URLs you can paste into PRs, Teams, or email.

## Security notes

- The secret contains OAuth tokens for your OneDrive. Treat `RCLONE_CONFIG_B64` like a password.
- Use a dedicated OneDrive folder (default `CheckEngine/AgentArtifacts`) and revoke the rclone app in Microsoft account settings if you rotate credentials.
- Share links created by `rclone link` are viewable by anyone with the URL until you remove sharing in OneDrive.

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `OneDrive is not configured` | Add `RCLONE_CONFIG_B64` (+ `_2`, `_3` if split) and start a new agent |
| `Secret value exceeds max length of 4096` | Run `scripts/split-rclone-config-for-cursor.sh` on your PC; add each printed chunk as its own secret |
| `RCLONE_CONFIG_B64 secret(s) are invalid` | Secrets are not your real `rclone.conf` — re-run the splitter on your PC; first chunk must decode to `[onedrive]` |
| `token expired` | Re-run `rclone config reconnect onedrive:` on your PC, re-encode config, update secret |
| `rclone: command not found` | Environment install script did not run — Save install=`bash .cursor/scripts/install.sh` and start a new agent |
| Upload works but no link | Some tenant policies block anonymous links; share the folder manually in OneDrive |
