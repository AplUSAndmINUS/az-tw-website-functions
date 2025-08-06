#!/bin/pwsh

Write-Host "Running Media Service Unit Tests..." -ForegroundColor Cyan

# Move to the MediaTests directory
$testsDir = Join-Path $PSScriptRoot "MediaTests"
Set-Location $testsDir

# Build the project first
Write-Host "Building MediaTests project..." -ForegroundColor Yellow
dotnet build

# Run the tests
Write-Host "Running tests..." -ForegroundColor Yellow
dotnet test

# Check the test results
if ($LASTEXITCODE -eq 0) {
    Write-Host "Media service tests completed successfully!" -ForegroundColor Green
} else {
    Write-Host "Media service tests failed with exit code: $LASTEXITCODE" -ForegroundColor Red
}

# Return to original directory
Set-Location $PSScriptRoot
