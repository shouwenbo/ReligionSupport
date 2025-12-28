# 下载 yt-dlp.exe 到程序目录
$ytDlpUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe"
$outputPath = Join-Path $PSScriptRoot "yt-dlp.exe"

Write-Host "正在下载 yt-dlp.exe..." -ForegroundColor Green
Write-Host "下载地址: $ytDlpUrl" -ForegroundColor Cyan
Write-Host "保存位置: $outputPath" -ForegroundColor Cyan

try {
    Invoke-WebRequest -Uri $ytDlpUrl -OutFile $outputPath -UseBasicParsing
    Write-Host "`n下载完成！" -ForegroundColor Green
    Write-Host "文件大小: $((Get-Item $outputPath).Length / 1MB) MB" -ForegroundColor Yellow
} catch {
    Write-Host "`n下载失败: $_" -ForegroundColor Red
    Write-Host "`n请手动下载并放置到程序目录：" -ForegroundColor Yellow
    Write-Host $ytDlpUrl -ForegroundColor Cyan
}

Read-Host "`n按任意键退出"
