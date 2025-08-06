#!/bin/pwsh

# Configuration
$projectRoot = $PSScriptRoot
$testProject = Join-Path $projectRoot "Tests" "Function.Tests.csproj"
$reportDir = Join-Path $projectRoot "TestResults"
$reportFile = Join-Path $reportDir "media-test-report.html"
$coverageFile = Join-Path $reportDir "coverage.xml"
$packageSource = "https://api.nuget.org/v3/index.json"

# Create the report directory if it doesn't exist
if (-not (Test-Path $reportDir)) {
    New-Item -Path $reportDir -ItemType Directory | Out-Null
}

# Install required tools if not already installed
function Install-ToolIfNotExists {
    param (
        [string]$ToolName,
        [string]$PackageName
    )
    
    if (-not (dotnet tool list --global | Select-String -Pattern $ToolName)) {
        Write-Host "Installing $ToolName..." -ForegroundColor Cyan
        dotnet tool install --global $PackageName --add-source $packageSource
    } else {
        Write-Host "$ToolName is already installed" -ForegroundColor Green
    }
}

Install-ToolIfNotExists -ToolName "reportgenerator" -PackageName "dotnet-reportgenerator-globaltool"
Install-ToolIfNotExists -ToolName "coverlet" -PackageName "coverlet.console"

# Run tests with coverage
Write-Host "Running tests with coverage..." -ForegroundColor Cyan
dotnet test $testProject --filter "FullyQualifiedName~Media" `
    --collect:"XPlat Code Coverage" `
    --results-directory:$reportDir `
    --logger:trx

# Find the coverage file
$coverageGlob = Join-Path $reportDir "*\coverage.cobertura.xml"
$coverageFiles = Get-ChildItem -Path $coverageGlob
if ($coverageFiles.Count -eq 0) {
    Write-Host "No coverage files found!" -ForegroundColor Red
    exit 1
}
$actualCoverageFile = $coverageFiles[0].FullName

# Generate the report
Write-Host "Generating test report..." -ForegroundColor Cyan
reportgenerator `
    "-reports:$actualCoverageFile" `
    "-targetdir:$reportDir" `
    "-reporttypes:Html;Cobertura" `
    "-title:Media Services Test Coverage"

# Open the report
if (Test-Path (Join-Path $reportDir "index.html")) {
    $reportUrl = (Join-Path $reportDir "index.html")
    Write-Host "Opening report: $reportUrl" -ForegroundColor Green
    Start-Process $reportUrl
} else {
    Write-Host "Report file not found!" -ForegroundColor Red
    exit 1
}

# Extract test results
$trxFiles = Get-ChildItem -Path (Join-Path $reportDir "*.trx")
if ($trxFiles.Count -gt 0) {
    [xml]$trxContent = Get-Content $trxFiles[0].FullName
    $totalTests = $trxContent.TestRun.ResultSummary.Counters.total
    $passedTests = $trxContent.TestRun.ResultSummary.Counters.passed
    $failedTests = $trxContent.TestRun.ResultSummary.Counters.failed
    
    Write-Host "Test Results Summary:" -ForegroundColor Cyan
    Write-Host "  Total Tests: $totalTests" -ForegroundColor White
    Write-Host "  Passed: $passedTests" -ForegroundColor Green
    Write-Host "  Failed: $failedTests" -ForegroundColor $(if ($failedTests -gt 0) { "Red" } else { "Green" })
}

# Return exit code based on test success
if ($failedTests -gt 0) {
    exit 1
} else {
    exit 0
}
