# VideoFetchApp 发布脚本
# 用于创建干净的发布包，包含所有必需的依赖项

# 设置控制台输出编码为 UTF-8，避免中文乱码
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8
chcp 65001 | Out-Null

$ErrorActionPreference = "Stop"

Write-Host "====================================" -ForegroundColor Cyan
Write-Host "VideoFetchApp 发布脚本" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# 1. 清理旧的输出
Write-Host "[1/4] 清理旧的输出文件..." -ForegroundColor Yellow
dotnet clean -c Release
if (Test-Path ".\发布") {
    Remove-Item ".\发布" -Recurse -Force
    Write-Host "      已删除旧的发布文件夹" -ForegroundColor Gray
}
Write-Host "      清理完成" -ForegroundColor Green
Write-Host ""

# 2. 编译 Release 版本
Write-Host "[2/4] 编译 Release 版本..." -ForegroundColor Yellow
dotnet build -c Release
Write-Host "      编译完成" -ForegroundColor Green
Write-Host ""

# 3. 发布项目
Write-Host "[3/4] 发布项目..." -ForegroundColor Yellow
dotnet publish -c Release -o ".\发布"
Write-Host "      发布完成" -ForegroundColor Green
Write-Host ""

# 4. 验证必需文件
Write-Host "[4/4] 验证发布内容..." -ForegroundColor Yellow
$publishPath = ".\发布"
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
        Write-Host "      ✓ $file ($([math]::Round($fileSize, 2)) MB)" -ForegroundColor Green
    } else {
        Write-Host "      ✗ $file (缺失)" -ForegroundColor Red
        $allFilesExist = $false
    }
}

Write-Host ""
if ($allFilesExist) {
    Write-Host "====================================" -ForegroundColor Cyan
    Write-Host "发布成功！" -ForegroundColor Green
    Write-Host "====================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "发布目录: $publishPath" -ForegroundColor White
    Write-Host ""
    
    # 计算总大小
    $totalSize = (Get-ChildItem $publishPath -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
    Write-Host "发布包总大小: $([math]::Round($totalSize, 2)) MB" -ForegroundColor White
    Write-Host ""
    Write-Host "可以将 '发布' 文件夹打包分发给用户了。" -ForegroundColor Yellow
} else {
    Write-Host "====================================" -ForegroundColor Cyan
    Write-Host "发布失败！" -ForegroundColor Red
    Write-Host "====================================" -ForegroundColor Cyan
    Write-Host "某些必需文件缺失，请检查项目配置。" -ForegroundColor Red
    exit 1
}
