# 快速启动指南

## 🚀 立即使用

### 方法一：直接运行已发布的 EXE
1. 导航到发布文件夹：
   ```
   PhotoBGCheck.WPF\bin\Release\net8.0-windows\win-x64\publish\
   ```
2. 双击 `PhotoBGCheck.WPF.exe` 即可运行
3. **无需安装任何依赖**，EXE 已包含所有必需组件

### 方法二：在 Visual Studio 中调试运行
1. 打开 `ReligionSupport.sln`
2. 在解决方案资源管理器中找到 `PhotoBGCheck.WPF` 项目
3. 右键 → 设为启动项目
4. 按 `F5` 开始调试

## 📦 重新发布

如果您修改了代码需要重新发布：

### PowerShell 命令行（推荐）
```powershell
cd PhotoBGCheck.WPF
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishReadyToRun=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

### Visual Studio
1. 右键项目 → 发布
2. 选择 `FolderProfile` 配置
3. 点击"发布"按钮
4. 等待完成

发布的 EXE 位于：
```
bin\Release\net8.0-windows\win-x64\publish\PhotoBGCheck.WPF.exe
```

## 📖 文档

- **[README.md](README.md)** - 完整技术文档
- **[USAGE.md](USAGE.md)** - 用户使用指南
- **[PROJECT_SUMMARY.md](PROJECT_SUMMARY.md)** - 项目总结

## ⚙️ 系统要求

- **操作系统**：Windows 10 或更高版本
- **架构**：x64
- **依赖**：无（自包含发布）

## 🎯 第一次使用

1. 运行 `PhotoBGCheck.WPF.exe`
2. 点击"选择文件夹"，选择包含证件照的文件夹
3. 点击"开始检测"
4. 查看检测结果

就这么简单！

## 💡 提示

- 首次使用建议用少量照片（10-20张）测试
- 熟悉功能后再批量检测大量照片
- 可以根据实际需求调整检测参数

## 🐛 问题反馈

如遇到任何问题，请检查：
1. 照片格式是否为 JPG/PNG
2. 文件夹是否有读取权限
3. Windows 版本是否为 10 或更高

---

**准备就绪，开始使用吧！** 🎉
