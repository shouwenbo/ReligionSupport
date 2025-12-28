# Download Assets Script
# Automatically downloads required tools for VideoFetchApp

$ErrorActionPreference = "Stop"
$assetsPath = ".\Assets"

Write-Host "====================================" -ForegroundColor Cyan
Write-Host "VideoFetchApp Assets Download" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Create Assets folder if not exists
if (-not (Test-Path $assetsPath)) {
    New-Item -ItemType Directory -Path $assetsPath | Out-Null
    Write-Host "Created Assets folder" -ForegroundColor Green
}

# Download yt-dlp.exe
$ytdlpPath = Join-Path $assetsPath "yt-dlp.exe"
if (Test-Path $ytdlpPath) {
    Write-Host "[1/3] yt-dlp.exe already exists" -ForegroundColor Yellow
} else {
    Write-Host "[1/3] Downloading yt-dlp.exe..." -ForegroundColor Yellow
    try {
        Invoke-WebRequest -Uri "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe" `
            -OutFile $ytdlpPath -UseBasicParsing
        $size = (Get-Item $ytdlpPath).Length / 1MB
        Write-Host "      Downloaded successfully ($([math]::Round($size, 2)) MB)" -ForegroundColor Green
    } catch {
        Write-Host "      Download failed: $_" -ForegroundColor Red
    }
}

Write-Host ""

# Check ffmpeg.exe
$ffmpegPath = Join-Path $assetsPath "ffmpeg.exe"
if (Test-Path $ffmpegPath) {
    $size = (Get-Item $ffmpegPath).Length / 1MB
    Write-Host "[2/3] ffmpeg.exe already exists ($([math]::Round($size, 2)) MB)" -ForegroundColor Green
} else {
    Write-Host "[2/3] ffmpeg.exe not found" -ForegroundColor Yellow
    Write-Host "      Please download manually from:" -ForegroundColor White
    Write-Host "      https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip" -ForegroundColor Cyan
    Write-Host "      Extract ffmpeg.exe to: $assetsPath" -ForegroundColor White
}

Write-Host ""

# Check ffprobe.exe
$ffprobePath = Join-Path $assetsPath "ffprobe.exe"
if (Test-Path $ffprobePath) {
    $size = (Get-Item $ffprobePath).Length / 1MB
    Write-Host "[3/3] ffprobe.exe already exists ($([math]::Round($size, 2)) MB)" -ForegroundColor Green
} else {
    Write-Host "[3/3] ffprobe.exe not found" -ForegroundColor Yellow
    Write-Host "      Please download manually from:" -ForegroundColor White
    Write-Host "      https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip" -ForegroundColor Cyan
    Write-Host "      Extract ffprobe.exe to: $assetsPath" -ForegroundColor White
}

Write-Host ""
Write-Host "====================================" -ForegroundColor Cyan

# Final check
$allFilesExist = (Test-Path $ytdlpPath) -and (Test-Path $ffmpegPath) -and (Test-Path $ffprobePath)
if ($allFilesExist) {
    Write-Host "All assets ready!" -ForegroundColor Green
    Write-Host "====================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "You can now build the project." -ForegroundColor Yellow
} else {
    Write-Host "Some assets are missing" -ForegroundColor Yellow
    Write-Host "====================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Please download missing files manually." -ForegroundColor Yellow
    Write-Host "See Assets\README.md for detailed instructions." -ForegroundColor White
}

Write-Host ""
