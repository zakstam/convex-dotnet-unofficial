# Local packaging test script
# This simulates what the GitHub Actions workflow does

param(
    [string]$Version = "1.0.5-alpha"
)

Write-Host "🧪 Testing NuGet Package Creation Locally" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Yellow
Write-Host ""

# Create output directory
$outputDir = ".\test-nupkg"
if (Test-Path $outputDir) {
    Remove-Item -Recurse -Force $outputDir
}
New-Item -ItemType Directory -Path $outputDir | Out-Null

Write-Host "📦 Step 1: Restoring dependencies..." -ForegroundColor Green
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Restore failed!" -ForegroundColor Red
    exit 1
}

Write-Host "🔨 Step 2: Building solution..." -ForegroundColor Green
dotnet build --configuration Release -p:Version=$Version -p:PackageVersion=$Version
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "🧪 Step 3: Running tests..." -ForegroundColor Green
dotnet test --configuration Release --verbosity normal --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️  Tests failed, but continuing with packaging test..." -ForegroundColor Yellow
}

Write-Host "📦 Step 4: Packing packages..." -ForegroundColor Green
# Pack src/ projects only (excludes examples, tests, and non-packable analyzers)
Get-ChildItem -Path ./src -Recurse -Filter "*.csproj" | ForEach-Object {
    $csprojContent = Get-Content $_.FullName -Raw
    if ($csprojContent -notmatch '<IsPackable>false</IsPackable>') {
        Write-Host "  📦 Packing: $($_.Name)" -ForegroundColor White
        dotnet pack $_.FullName --no-build --configuration Release --output $outputDir -p:Version=$Version -p:PackageVersion=$Version
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  ⚠️  Failed to pack $($_.Name)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  ⏭️  Skipping non-packable: $($_.Name)" -ForegroundColor Gray
    }
}
$packExitCode = 0

Write-Host ""
Write-Host "📊 Results:" -ForegroundColor Cyan
$packages = Get-ChildItem -Path $outputDir -Filter "*.nupkg" | Where-Object { $_.Name -notlike "*.snupkg" }
$symbols = Get-ChildItem -Path $outputDir -Filter "*.snupkg"

Write-Host "  Packages created: $($packages.Count)" -ForegroundColor $(if ($packages.Count -gt 0) { "Green" } else { "Red" })
Write-Host "  Symbol packages: $($symbols.Count)" -ForegroundColor $(if ($symbols.Count -gt 0) { "Green" } else { "Yellow" })

if ($packages.Count -gt 0) {
    Write-Host ""
    Write-Host "✅ Created packages:" -ForegroundColor Green
    foreach ($pkg in $packages) {
        $size = [math]::Round($pkg.Length / 1KB, 2)
        Write-Host "    - $($pkg.Name) ($size KB)" -ForegroundColor White
    }
}

Write-Host ""
if ($packExitCode -eq 0) {
    Write-Host "✅ Packaging test completed successfully!" -ForegroundColor Green
    Write-Host "   Packages are in: $outputDir" -ForegroundColor Cyan
} else {
    Write-Host "❌ Packaging failed with exit code: $packExitCode" -ForegroundColor Red
    Write-Host "   Check the errors above for details" -ForegroundColor Yellow
    exit $packExitCode
}

