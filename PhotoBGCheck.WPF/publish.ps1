# 发布脚本 - PhotoBGCheck.WPF
# 用于生成单文件 EXE

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "证件照背景检测工具 - 发布脚本" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 项目路径
$projectPath = $PSScriptRoot
$projectFile = Join-Path $projectPath "PhotoBGCheck.WPF.csproj"

# 检查项目文件是否存在
if (-not (Test-Path $projectFile)) {
    Write-Host "❌ 错误: 找不到项目文件 $projectFile" -ForegroundColor Red
    exit 1
}

Write-Host "📁 项目路径: $projectPath" -ForegroundColor Green
Write-Host ""

# 清理之前的发布文件
Write-Host "🧹 清理旧的发布文件..." -ForegroundColor Yellow
$publishPath = Join-Path $projectPath "bin\Release\net8.0-windows\publish"
if (Test-Path $publishPath) {
    Remove-Item -Path $publishPath -Recurse -Force
    Write-Host "✅ 清理完成" -ForegroundColor Green
}
Write-Host ""

# 发布项目
Write-Host "🔨 开始发布项目..." -ForegroundColor Yellow
Write-Host "   配置: Release" -ForegroundColor Gray
Write-Host "   目标: win-x64" -ForegroundColor Gray
Write-Host "   模式: 单文件 + 自包含" -ForegroundColor Gray
Write-Host ""

$publishArgs = @(
    "publish",
    $projectFile,
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:PublishReadyToRun=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:DebugType=None",
    "-p:DebugSymbols=false"
)

$result = & dotnet $publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ 发布失败！" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✅ 发布成功！" -ForegroundColor Green
Write-Host ""

# 显示发布文件信息
$exePath = Join-Path $publishPath "PhotoBGCheck.WPF.exe"
if (Test-Path $exePath) {
    $fileInfo = Get-Item $exePath
    $fileSizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
    
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "📦 发布文件信息" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "文件名: $($fileInfo.Name)" -ForegroundColor White
    Write-Host "大小: $fileSizeMB MB" -ForegroundColor White
    Write-Host "路径: $($fileInfo.FullName)" -ForegroundColor White
    Write-Host ""
    
    # 询问是否打开文件夹
    Write-Host "是否打开发布文件夹？ (Y/N): " -ForegroundColor Yellow -NoNewline
    $response = Read-Host
    if ($response -eq 'Y' -or $response -eq 'y') {
        Start-Process "explorer.exe" -ArgumentList $publishPath
    }
} else {
    Write-Host "⚠️  警告: 找不到生成的 EXE 文件" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✨ 发布流程完成！" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
