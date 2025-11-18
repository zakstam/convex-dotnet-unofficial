#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Reorganize solution with proper folder structure
.DESCRIPTION
    Adds solution folders and organizes projects logically
#>

$ErrorActionPreference = "Stop"

Write-Host "🗂️  Reorganizing Solution Structure" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""

$solutionFile = "convex-dotnet-client.sln"

# Backup solution file
Write-Host "📋 Creating backup..." -ForegroundColor Yellow
Copy-Item $solutionFile "$solutionFile.backup"

# Add solution folders using Visual Studio solution format
Write-Host "📁 Adding solution folders..." -ForegroundColor Green

# Note: dotnet sln doesn't support adding empty folders via CLI
# We'll modify the .sln file directly

$slnContent = Get-Content $solutionFile -Raw

# Check if solution folders already exist
if ($slnContent -match 'Testing Infrastructure') {
    Write-Host "⚠️  Solution folders already exist" -ForegroundColor Yellow
    exit 0
}

# Add testing infrastructure folder
$testingFolderGuid = "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"
$slnContent = $slnContent -replace '(Project\("\{2150E333-8FDC-42A3-9474-1A3956D46DE8\}"\) = "tests")', @"
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Testing Infrastructure", "Testing Infrastructure", "$testingFolderGuid"
EndProject
`$1
"@

# Add benchmarks folder to solution if needed
$benchmarksFolderGuid = "{8B19103F-16F7-4668-BE54-9A1E7A4F7557}"
if ($slnContent -notmatch 'benchmarks') {
    $slnContent = $slnContent -replace '(Project\("\{2150E333-8FDC-42A3-9474-1A3956D46DE8\}"\) = "tools")', @"
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "benchmarks", "benchmarks", "$benchmarksFolderGuid"
EndProject
`$1
"@
}

# Save modified solution
Set-Content $solutionFile $slnContent -NoNewline

Write-Host ""
Write-Host "✅ Solution reorganized successfully!" -ForegroundColor Green
Write-Host "   Backup saved to: $solutionFile.backup" -ForegroundColor Gray
