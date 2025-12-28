# Assets 文件夹说明

此文件夹应包含以下必需的可执行文件（因文件过大未上传到 Git）：

## 必需文件

1. **ffmpeg.exe** (~128 MB)
   - 用途：合并视频和音频流
   - 下载地址：https://ffmpeg.org/download.html
   - 推荐版本：最新 Windows 64-bit GPL 版本

2. **ffprobe.exe** (~128 MB)
   - 用途：分析视频信息
   - 下载地址：与 ffmpeg 在同一包中
   - 位置：ffmpeg 压缩包的 bin 文件夹中

3. **yt-dlp.exe** (~18 MB)
   - 用途：下载 YouTube 视频
   - 下载地址：https://github.com/yt-dlp/yt-dlp/releases/latest
   - 文件：yt-dlp.exe（Windows 可执行文件）

## 如何获取

### 方法 1：手动下载

1. 访问上述下载地址
2. 下载对应的可执行文件
3. 将文件放置到 `VideoFetchApp\Assets\` 文件夹中

### 方法 2：使用下载脚本（Windows）

在 VideoFetchApp 文件夹中创建并运行以下 PowerShell 脚本：

```powershell
# download-assets.ps1
$assetsPath = ".\Assets"
New-Item -ItemType Directory -Force -Path $assetsPath | Out-Null

# 下载 yt-dlp
Write-Host "Downloading yt-dlp.exe..."
Invoke-WebRequest -Uri "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe" `
    -OutFile "$assetsPath\yt-dlp.exe"

Write-Host "Please download ffmpeg manually from: https://ffmpeg.org/download.html"
Write-Host "Extract ffmpeg.exe and ffprobe.exe to: $assetsPath"
```

## 文件结构

完成后，Assets 文件夹应包含：

```
Assets/
├── ffmpeg.exe     (~128 MB)
├── ffprobe.exe    (~128 MB)
└── yt-dlp.exe     (~18 MB)
```

## 验证

运行项目前，确保所有三个文件都已放置在 Assets 文件夹中。
编译时这些文件会自动复制到输出目录的 Assets 文件夹。
