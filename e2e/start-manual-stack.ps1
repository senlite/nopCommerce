param(
    [string]$BrowserZip = (Join-Path $PSScriptRoot '.browser\chromium-win64.zip'),
    [string]$BrowserExtractDir = (Join-Path $PSScriptRoot '.browser')
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $BrowserExtractDir)) {
    New-Item -ItemType Directory -Force -Path $BrowserExtractDir | Out-Null
}

$browserChrome = Join-Path $BrowserExtractDir 'chrome-win\chrome.exe'
if (-not (Test-Path $browserChrome)) {
    if (-not (Test-Path $BrowserZip)) {
        throw "Chromium ZIP not found at $BrowserZip"
    }

    Expand-Archive -Path $BrowserZip -DestinationPath $BrowserExtractDir -Force
}

$networkExists = $false
try {
    podman network inspect checkengine-e2e | Out-Null
    $networkExists = $true
} catch {
    $networkExists = $false
}

if (-not $networkExists) {
    podman network create checkengine-e2e | Out-Null
}

podman build -t e2e-web -f e2e/Dockerfile.web .

podman rm -f checkengine-e2e-db checkengine-e2e-web 2>$null | Out-Null
podman run -d --name checkengine-e2e-db --network checkengine-e2e -e POSTGRES_PASSWORD='nopCommerce_db_password123!' -e POSTGRES_USER=nopCommerce -e POSTGRES_DB=nopCommerce -p 5432:5432 postgres:16 | Out-Null
podman run -d --name checkengine-e2e-web --network checkengine-e2e -e ASPNETCORE_ENVIRONMENT=Production -e ASPNETCORE_URLS=http://0.0.0.0:5000 -e E2E_CONNECTION_STRING='Host=checkengine-e2e-db;Port=5432;Database=nopCommerce;Username=nopCommerce;Password=nopCommerce_db_password123!' -e E2E_DATA_PROVIDER=3 -p 5000:5000 e2e-web | Out-Null
Write-Host 'Manual stack started. Browse http://127.0.0.1:5000'
Write-Host 'Stop with: podman rm -f checkengine-e2e-web checkengine-e2e-db'
