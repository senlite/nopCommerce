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

### 4. Add Cursor environment secrets

Open your environment in the dashboard:

**Cursor → Cloud Agents → Environments → (your environment) → Secrets**

| Secret | Required | Example |
|--------|----------|---------|
| `RCLONE_CONFIG_B64` | Yes | (paste base64 from step 3) |
| `ONEDRIVE_ARTIFACTS_FOLDER` | No | `CheckEngine/AgentArtifacts` |
| `ONEDRIVE_RCLONE_REMOTE` | No | `onedrive:` (must match remote name in config) |

Save the environment and **trigger a new build** so future agents start with rclone installed.

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
| `OneDrive is not configured` | Add `RCLONE_CONFIG_B64` and restart / rebuild the environment |
| `token expired` | Re-run `rclone config reconnect onedrive:` on your PC, re-encode config, update secret |
| `rclone: command not found` | Environment build missing `.cursor/environment.json` install step — rebuild environment |
| Upload works but no link | Some tenant policies block anonymous links; share the folder manually in OneDrive |
