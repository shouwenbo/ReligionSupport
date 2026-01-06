# TextOCR.WPF

智能视频字幕提取工具 - 基于 PaddleOCR 的高精度字幕识别系统

## 功能特性

- ✨ 现代化的 Material Design 界面
- 🎯 实时预览字幕识别效果
- ⚙️ 灵活的参数配置
- 💾 自动保存用户设置
- 📊 实时进度显示
- 🚀 高性能视频处理

## 系统要求

- .NET 8.0 或更高版本
- Python 3.10+ (已安装 PaddleOCR 3.3.1)
- Windows 10/11

## 依赖版本

### Python 依赖
- **PaddleOCR 3.3.1** (最新稳定版)
- OpenCV-Python 4.9.0+

```bash
pip install paddleocr==3.3.1 opencv-python
```

### NuGet 包
- CommunityToolkit.Mvvm 8.2.2
- MaterialDesignThemes 5.0.0
- OpenCvSharp4 4.9.0
- Microsoft.Extensions.DependencyInjection 8.0.0

## 使用说明

1. **选择视频文件**
   - 点击"选择视频文件"按钮
   - 支持 MP4, AVI, MKV, MOV 格式

2. **配置参数**
   - 字幕区域高度：调整字幕截取区域的高度
   - 字幕位置：选择从顶部或底部截取
   - 帧间隔：设置采样频率
   - 预览时间点：指定预览的视频秒数

3. **预览效果**
   - 点击"预览识别效果"查看当前配置的识别结果
   - 可以多次调整参数并预览

4. **开始提取**
   - 点击"开始提取字幕"
   - 选择输出文件位置
   - 等待处理完成
   - 自动打开结果文件

## 项目结构

```
TextOCR.WPF/
├── Models/              # 数据模型
├── Services/            # 业务服务
│   ├── VideoProcessingService.cs
│   ├── PythonOcrService.cs
│   └── SettingsService.cs
├── ViewModels/          # 视图模型
│   └── MainViewModel.cs
├── Views/               # 视图
│   └── MainWindow.xaml
├── Converters/          # 值转换器
├── PythonScripts/       # Python 脚本
│   ├── recognize_text.py
│   └── extract_subtitles.py
└── Properties/          # 应用程序属性
    └── Settings.settings
```

## 技术架构

- **架构模式**: MVVM (Model-View-ViewModel)
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **UI 框架**: Material Design In XAML
- **视频处理**: OpenCvSharp4
- **OCR 引擎**: PaddleOCR (Python)

## 配置说明

所有用户配置都保存在 `Properties/Settings.settings` 中，包括：

- SubtitleHeight: 字幕区域高度
- FromTop: 字幕位置（顶部/底部）
- FrameInterval: 帧间隔
- PreviewSecond: 预览时间点
- LastVideoPath: 上次使用的视频路径

## 注意事项

- 请确保已正确安装 Python 和 PaddleOCR
- 如果 Python 安装路径不同，请修改 `PythonOcrService.cs` 中的路径配置
- 中文用户名可能导致路径问题，程序已自动设置模型路径为 `C:/PaddleOCR_Models`

## 构建和运行

```bash
# 还原 NuGet 包
dotnet restore

# 构建项目
dotnet build

# 运行应用程序
dotnet run
```

## 许可证

本项目采用 MIT 许可证
