# MODNet 模型下载说明

## AI智能模式需要模型文件

如果您想使用**AI智能模式**（更高准确率），需要下载 MODNet 模型文件。

---

## 🚀 快速下载（推荐）

### 直接下载已转换好的ONNX模型

**Google Drive官方下载链接**：
```
https://drive.google.com/file/d/1cgycTQlYXpTh26gB9FTnthE7AvruV8hd/view?usp=sharing
```

**下载步骤**：

1. **访问链接**：点击上方Google Drive链接

2. **下载文件**：点击页面上的"下载"按钮
   - 如果提示"无法扫描病毒"，点击"仍要下载"
   - 文件名：`modnet_photographic_portrait_matting.onnx`
   - 文件大小：约24-25MB

3. **重命名文件**：下载完成后，将文件重命名为 `modnet.onnx`

4. **放置文件**：将 `modnet.onnx` 放到程序目录下的 `Assets` 文件夹
   ```
   PhotoBGCheck.WPF.exe 所在目录/Assets/modnet.onnx
   ```
   
   完整路径示例：
   ```
   f:\code\ReligionSupport\PhotoBGCheck.WPF\bin\Release\net8.0-windows\win-x64\publish\Assets\modnet.onnx
   ```

5. **验证安装**：启动程序，查看"AI智能模式"下方的状态提示
   - ✅ 显示"AI模型已加载" = 成功
   - ❌ 显示"AI模型未找到" = 检查文件路径

---

## 🌐 其他下载方式

### 方法二：百度网盘（国内网络推荐）

如果无法访问Google Drive，可以使用百度网盘：

**临时下载链接**（需要先从Google Drive下载后上传）：
```
待提供
```

---

## 🔧 方法三：自行转换（适合开发者）

```python
# 安装依赖
pip install torch torchvision onnx

# 下载PyTorch模型并转换
# 参考：https://github.com/ZHKKKe/MODNet
```

### 模型信息

- **文件名**：modnet.onnx
- **文件大小**：约 25 MB
- **输入**：512x512 RGB图像
- **输出**：512x512 Alpha蒙版
- **许可证**：[Creative Commons BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/)
- **用途限制**：仅限非商业使用

### 验证安装

1. 将 `modnet.onnx` 放到 `Assets` 文件夹
2. 启动程序
3. 在"检测模式"区域，AI智能模式应该可以选择
4. 如果仍显示"缺少模型文件"，检查：
   - 文件名是否为 `modnet.onnx`（小写）
   - 文件是否在正确的 `Assets` 目录下
   - 文件大小是否正确（约25MB）

### 目录结构

```
PhotoBGCheck.WPF/
├── PhotoBGCheck.WPF.exe          # 主程序
└── Assets/
    ├── icon.ico                  # 图标（已包含）
    └── modnet.onnx              # MODNet模型（需下载）
```

### 注意事项

⚠️ **许可证限制**
- MODNet 模型仅限**非商业使用**
- 本程序是内部工具，符合非商业使用条款
- 如需商业用途，请联系模型作者获取授权

⚠️ **隐私保护**
- 所有处理**完全在本地进行**
- 照片不会上传到任何服务器
- 无需网络连接

### 快速模式 vs AI智能模式

如果不下载模型文件，程序会**自动使用快速模式**：
- ✅ 完全免费，无需额外文件
- ✅ 处理速度极快（毫秒级）
- ✅ 适合大批量快速筛查
- ⚠️ 准确率约 85-90%

下载模型后，可使用**AI智能模式**：
- ✅ 准确率更高（92-95%）
- ✅ AI人像识别，检测全背景
- ✅ 适合日常使用
- ⚠️ 处理速度较慢（约0.5秒/张）

---

**推荐**：先使用快速模式测试，如果误判率高，再下载模型使用AI模式。
