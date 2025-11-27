# Settings 配置参考

| 项目 | 类型 | 默认值 | 描述 |
| --- | --- | --- | --- |
| LastVideoPath | string | 空 | 上次选择的视频路径。 |
| RecentFiles | StringCollection | 空 | 最近 10 个文件。 |
| FrameInterval | int | 30 | 帧提取间隔。 |
| HashThreshold | int | 5 | pHash 差异阈值。 |
| TesseractPath | string | `C:\Program Files\Tesseract-OCR\tesseract.exe` | OCR 引擎路径。 |
| OcrLanguages | string | `eng+chi_sim` | OCR 语言列表。 |
| OutputDirectory | string | `%USERPROFILE%\Documents\SlideExtractor` | 默认输出目录。 |
| WindowWidth/Height/Left/Top | double | 1280/800/100/100 | 窗口尺寸与位置。 |
| Theme | string | Dark | UI 主题。 |
| ThumbnailSize | int | 180 | 缩略图大小。 |
| ThumbnailColumns | int | 4 | 缩略图列数。 |
| AutoSaveEnabled | bool | true | 自动保存结果。 |
| LastExportFormat | string | PPTX | 最近导出格式。 |
| DefaultNamingRule | string | `[date]_{video}_{index}` | 命名规则。 |
| TempRetentionHours | int | 24 | 临时文件保留时间。 |
| DragDropEnabled | bool | true | 是否启用拖拽。 |
| IsFirstRun | bool | true | 首次运行标记。 |
| UiLanguage | string | zh-CN | 界面语言。 |
| ExportQuality | string | High | 导出质量。 |
| ImageFormat | string | PNG | 图片格式。 |
| ImageQuality | int | 90 | 图片质量。 |
| EnableBatchMode | bool | false | 批处理模式。 |
| MaxConcurrentTasks | int | 4 | 最大并发。 |
| MemoryLimitMB | int | 2048 | 内存限制。 |
| LogLevel | string | Information | 日志级别。 |
