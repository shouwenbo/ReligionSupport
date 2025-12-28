# VideoFetchApp Build & Publish Script
# Creates a clean release package with all required dependencies

# Set UTF-8 encoding to avoid encoding issues
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$ErrorActionPreference = "Stop"

Write-Host "====================================" -ForegroundColor Cyan
Write-Host "VideoFetchApp Build Script" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# 1. Clean old outputs
Write-Host "[1/4] Cleaning old output files..." -ForegroundColor Yellow
dotnet clean -c Release
if (Test-Path ".\publish") {
    Remove-Item ".\publish" -Recurse -Force
    Write-Host "      Removed old publish folder" -ForegroundColor Gray
}
Write-Host "      Clean completed" -ForegroundColor Green
Write-Host ""

# 2. Build Release version
Write-Host "[2/4] Building Release version..." -ForegroundColor Yellow
dotnet build -c Release
Write-Host "      Build completed" -ForegroundColor Green
Write-Host ""

# 3. Publish project
Write-Host "[3/4] Publishing project..." -ForegroundColor Yellow
dotnet publish -c Release -o ".\publish"
Write-Host "      Publish completed" -ForegroundColor Green
Write-Host ""

# 4. Verify required files
Write-Host "[4/4] Verifying publish content..." -ForegroundColor Yellow
$publishPath = ".\publish"
$requiredFiles = @(
    "VideoFetchApp.exe",
    "Assets\ffmpeg.exe",
    "Assets\ffprobe.exe",
    "Assets\yt-dlp.exe"
)

$allFilesExist = $true
foreach ($file in $requiredFiles) {
    $fullPath = Join-Path $publishPath $file
    if (Test-Path $fullPath) {
        $fileSize = (Get-Item $fullPath).Length / 1MB
        Write-Host "      OK $file ($([math]::Round($fileSize, 2)) MB)" -ForegroundColor Green
    } else {
        Write-Host "      MISSING $file" -ForegroundColor Red
        $allFilesExist = $false
    }
}

Write-Host ""
if ($allFilesExist) {
    Write-Host "====================================" -ForegroundColor Cyan
    Write-Host "Publish Successful!" -ForegroundColor Green
    Write-Host "====================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Publish directory: $publishPath" -ForegroundColor White
    Write-Host ""
    
    # Calculate total size
    $totalSize = (Get-ChildItem $publishPath -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
    Write-Host "Total package size: $([math]::Round($totalSize, 2)) MB" -ForegroundColor White
    Write-Host ""
    Write-Host "You can now package the 'publish' folder for distribution." -ForegroundColor Yellow
} else {
    Write-Host "====================================" -ForegroundColor Cyan
    Write-Host "Publish Failed!" -ForegroundColor Red
    Write-Host "====================================" -ForegroundColor Cyan
    Write-Host "Some required files are missing. Please check project configuration." -ForegroundColor Red
    exit 1
}
