# 架构说明

## 项目结构
```
SlideExtractor.WPF
├── App.xaml / App.xaml.cs
├── Models
├── Services
├── ViewModels
├── Views
├── Helpers
├── Resources
└── Properties (Settings)
```

## MVVM
- View：`MainWindow`, `SlideSelectionView`, `SettingsView` 只负责 UI。
- ViewModel：`MainViewModel` 统筹流程，`SlideSelectionViewModel` 与 `SettingsViewModel` 负责各自区域状态。
- Model：`SlideModel`, `VideoSourceModel`, `ExtractionSettings` 描述数据。

## 依赖注入
`App.xaml.cs` 使用 `Host.CreateDefaultBuilder` 并注册所有服务（帧提取、去重、OCR、导出、日志、助手）。ViewModel 和 View 通过构造函数获取服务，实现单例/作用域管理。

## 服务职责
- `FrameExtractionService`：OpenCvSharp 帧抽取与临时存储。
- `SlideDeduplicationService`：Perceptual Hash 去重。
- `OcrService`：Tesseract OCR+语言配置。
- `PresentationService`：DocumentFormat.OpenXml 导出 PPT/图片。
- `LoggingService`：包装 Serilog。
- Helpers：文件对话框、拖拽、Settings、哈希。

## 日志
Serilog 写入 Debug 输出与 `%AppData%/SlideExtractor.WPF/logs/app-*.log`，保留 30 天。

## Settings
`Settings.settings` 保存所有用户配置；通过 `SettingsHelper` 线程安全访问并维护最近文件列表。VM 调用 `Settings.Default` 写入，应用退出时统一保存。

## 流程
1. 选择/拖拽 MP4。
2. `StartExtractionCommand` 创建 `CancellationToken`，依次调用帧提取 → 去重 → OCR。
3. Slides 绑定到 `SlideSelectionView`，用户选择后使用 `PresentationService` 导出。
4. 进度通过 `IProgress<T>` 回传 UI，支持取消。
