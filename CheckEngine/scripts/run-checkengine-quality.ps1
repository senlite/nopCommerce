param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$project = "src/Tests/TwinParticles.CheckEngine.Tests.Architecture/TwinParticles.CheckEngine.Tests.Architecture.csproj"
$resultsDir = "TestResults"
$trxName = "checkengine-tests.trx"
$summaryScript = "CheckEngine/scripts/summarize-checkengine-results.ps1"

Write-Host "[checkengine-quality] restore"
dotnet restore $project
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[checkengine-quality] build ($Configuration)"
dotnet build $project --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (Test-Path $resultsDir) {
    Remove-Item -Recurse -Force $resultsDir
}

Write-Host "[checkengine-quality] test + trx + coverage"
dotnet test $project --configuration $Configuration --no-build --logger "trx;LogFileName=$trxName" --collect:"XPlat Code Coverage" --results-directory ./$resultsDir --settings src/Tests/TwinParticles.CheckEngine.Tests.Architecture/coverage.runsettings --filter "FullyQualifiedName!~VectorMathTests" -v minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[checkengine-quality] Domain/Application coverage thresholds"
python3 CheckEngine/scripts/assert-checkengine-coverage.py --results-dir ./$resultsDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[checkengine-quality] summarize"
powershell -ExecutionPolicy Bypass -File $summaryScript -ResultsDir $resultsDir -TrxName $trxName
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[checkengine-quality] done. Results: ./$resultsDir/$trxName"
