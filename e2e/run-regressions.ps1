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

$root = (Get-Location).Path
$browserMount = (Resolve-Path $BrowserExtractDir).Path

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

podman rm -f checkengine-e2e-db checkengine-e2e-web checkengine-e2e-tests 2>$null | Out-Null

podman build -t e2e-web -f e2e/Dockerfile.web .
podman build -t e2e-tests -f e2e/Dockerfile.tests .

podman run -d --name checkengine-e2e-db --network checkengine-e2e -e POSTGRES_PASSWORD='nopCommerce_db_password123!' -e POSTGRES_USER=nopCommerce -e POSTGRES_DB=nopCommerce -p 5432:5432 postgres:16 | Out-Null
podman run -d --name checkengine-e2e-web --network checkengine-e2e -e ASPNETCORE_ENVIRONMENT=Production -e ASPNETCORE_URLS=http://0.0.0.0:5000 -p 5000:5000 -v "${root}:/src:Z" -w /src e2e-web dotnet run --project src/Presentation/Nop.Web/Nop.Web.csproj --no-restore --urls http://0.0.0.0:5000 | Out-Null
podman run --rm --name checkengine-e2e-tests --network checkengine-e2e -e E2E_BASE_URL=http://checkengine-e2e-web:5000 -e PLAYWRIGHT_BROWSER_PATH=/browser/chrome-win/chrome.exe -e E2E_ADMIN_EMAIL=admin@yourStore.com -e E2E_ADMIN_PASSWORD=Admin123$ -e E2E_CONNECTION_STRING='Host=checkengine-e2e-db;Port=5432;Database=nopCommerce;Username=nopCommerce;Password=nopCommerce_db_password123!' -e E2E_DATA_PROVIDER=3 -v "${root}:/src:Z" -v "${browserMount}:/browser:ro,Z" -w /src e2e-tests dotnet test src/Tests/TwinParticles.CheckEngine.Tests.E2E/TwinParticles.CheckEngine.Tests.E2E.csproj --filter "Install_And_Seed_Should_Complete|Home_Page_Should_Render|Search_Page_Should_Render_And_Return_Results|Product_Detail_Page_Should_Render_For_Known_Sample_Data" --logger trx --results-directory /tmp/testresults
$exitCode = $LASTEXITCODE

podman logs checkengine-e2e-web
podman stop checkengine-e2e-web checkengine-e2e-db | Out-Null
exit $exitCode
