#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build script for Convex .NET Client
.DESCRIPTION
    Builds all or specific projects in the solution
.PARAMETER Target
    Specific project/solution to build (default: all)
.PARAMETER Configuration
    Build configuration: Debug or Release (default: Debug)
.PARAMETER Clean
    Clean before building
.EXAMPLE
    .\build.ps1
    .\build.ps1 -Target Convex.Client -Configuration Release
    .\build.ps1 -Clean
#>

param(
    [string]$Target = "all",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

Write-Host "🔨 Convex .NET Client Build Script" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""

$rootDir = Split-Path -Parent $PSScriptRoot
$solutionFile = Join-Path $rootDir "convex-dotnet-client.sln"

# Clean if requested
if ($Clean) {
    Write-Host "🧹 Cleaning solution..." -ForegroundColor Yellow
    dotnet clean $solutionFile -c $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

# Build target
if ($Target -eq "all") {
    Write-Host "📦 Building entire solution ($Configuration)..." -ForegroundColor Green
    dotnet build $solutionFile -c $Configuration
} else {
    Write-Host "📦 Building $Target ($Configuration)..." -ForegroundColor Green
    $projectPath = Get-ChildItem -Recurse -Filter "$Target.csproj" | Select-Object -First 1
    if ($projectPath) {
        dotnet build $projectPath.FullName -c $Configuration
    } else {
        Write-Host "❌ Project '$Target' not found" -ForegroundColor Red
        exit 1
    }
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "✅ Build completed successfully!" -ForegroundColor Green
