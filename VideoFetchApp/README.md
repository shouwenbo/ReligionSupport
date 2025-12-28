# VideoFetchApp - YouTube 视频下载器

## 功能特点

- ✅ 支持 YouTube 视频下载
- ✅ 自动合并视频和音频流
- ✅ 实时显示下载进度
- ✅ 集成所有必需工具（ffmpeg、yt-dlp）
- ✅ 无需额外安装依赖

## 系统要求

- Windows 10/11
- .NET 8.0 Runtime（如果未安装，程序会提示下载）

## 首次设置（开发者）

如果从 Git 克隆项目，需要先下载必需的大文件（未包含在 Git 仓库中）：

### 自动下载（推荐）
```powershell
.\download-assets.ps1
```

### 手动下载
1. **yt-dlp.exe** - 从 https://github.com/yt-dlp/yt-dlp/releases/latest 下载
2. **ffmpeg.exe & ffprobe.exe** - 从 https://www.gyan.dev/ffmpeg/builds/ 下载
3. 将文件放置到 `Assets` 文件夹中

详细说明请查看 `Assets\README.md`

## 使用方法

1. 运行 `VideoFetchApp.exe`
2. 在"视频链接"框中粘贴 YouTube 视频 URL
3. 点击"开始下载"按钮
4. 选择保存位置和文件名
5. 等待下载完成

## 项目结构

```
VideoFetchApp/
├── VideoFetchApp.exe          # 主程序
├── VideoFetchApp.dll          # 程序库
├── VideoFetchApp.runtimeconfig.json
├── VideoFetchApp.deps.json
└── Assets/                    # 依赖工具（自动包含）
    ├── ffmpeg.exe             # 视频处理工具
    ├── ffprobe.exe            # 视频分析工具
    └── yt-dlp.exe             # YouTube 下载工具
```

## 开发说明

### 发布程序

使用提供的发布脚本：

```powershell
.\发布脚本.ps1
```

或手动发布：

```powershell
dotnet publish -c Release -o .\发布
```

### 项目配置

程序已配置为自动将 Assets 文件夹中的工具复制到输出目录：

- `ffmpeg.exe` - 视频音频合并工具
- `ffprobe.exe` - 视频信息探测工具
- `yt-dlp.exe` - YouTube 下载工具

这些文件会在编译和发布时自动包含，无需手动复制。

### 依赖说明

- **YoutubeExplode**: 已移除，改用更可靠的 yt-dlp
- **yt-dlp**: 业界标准的 YouTube 下载工具
- **ffmpeg**: 用于合并视频和音频流

## 日志文件

程序会在运行目录生成 `log.txt` 文件，记录下载过程和错误信息。
点击主界面的"查看日志"按钮可以查看日志内容。

## 常见问题

**Q: 下载失败，提示 403 错误？**
A: YouTube 可能更新了防护机制，请尝试更新 yt-dlp.exe 到最新版本。

**Q: 视频没有音频？**
A: 检查 Assets 文件夹中是否包含 ffmpeg.exe，该工具用于合并视频和音频。

**Q: 程序无法启动？**
A: 确保已安装 .NET 8.0 Runtime，下载地址：https://dotnet.microsoft.com/download

## 更新依赖工具

### 更新 yt-dlp

下载最新版本：https://github.com/yt-dlp/yt-dlp/releases/latest
替换 `Assets\yt-dlp.exe` 文件

### 更新 ffmpeg

下载最新版本：https://ffmpeg.org/download.html
替换 `Assets\ffmpeg.exe` 和 `Assets\ffprobe.exe` 文件

## 许可证

本项目使用的第三方工具许可：
- yt-dlp: Unlicense
- ffmpeg: GPL v3

---

**版本**: 1.0.0  
**最后更新**: 2025-12-28
