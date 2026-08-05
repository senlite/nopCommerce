param(
    [string]$ComposeFile = 'e2e/compose.test.yml',
    [string]$BrowserPath = 'D:\Users\mabou\Downloads\chromium-win64\chrome-win\chrome.exe',
    [string]$BaseUrl = 'http://127.0.0.1:5000'
)

$ErrorActionPreference = 'Stop'

$containerName = 'checkengine-e2e-browser'

podman compose -f $ComposeFile up -d --build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$env:E2E_BASE_URL = $BaseUrl
$env:PLAYWRIGHT_BROWSER_PATH = $BrowserPath

dotnet test src/Tests/TwinParticles.CheckEngine.Tests.E2E/TwinParticles.CheckEngine.Tests.E2E.csproj --filter "Home_Page_Should_Render|Search_Page_Should_Render_And_Return_The_Search_Form|Product_Detail_Page_Should_Render_For_Known_Sample_Data|Install_Page_Should_Render_And_Save_Artifacts"
exit $LASTEXITCODE
