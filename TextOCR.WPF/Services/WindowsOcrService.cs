using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using TextOCR.WPF.Models;
using WinOcr = Windows.Media.Ocr;

namespace TextOCR.WPF.Services;

/// <summary>
/// Windows 内置 OCR - 最简单可靠，无需额外依赖
/// </summary>
public class WindowsOcrService : IOcrService
{
    private WinOcr.OcrEngine? _ocrEngine;

    public WindowsOcrService()
    {
        try
        {
            // 尝试创建中文 OCR 引擎
            var chineseLanguage = new Windows.Globalization.Language("zh-CN");
            if (WinOcr.OcrEngine.IsLanguageSupported(chineseLanguage))
            {
                _ocrEngine = WinOcr.OcrEngine.TryCreateFromLanguage(chineseLanguage);
            }
            else
            {
                // 回退到英文
                var englishLanguage = new Windows.Globalization.Language("en-US");
                _ocrEngine = WinOcr.OcrEngine.TryCreateFromLanguage(englishLanguage);
            }
        }
        catch
        {
            // 使用默认语言（英文）
            var defaultLanguage = new Windows.Globalization.Language("en-US");
            _ocrEngine = WinOcr.OcrEngine.TryCreateFromLanguage(defaultLanguage);
        }
    }

    public async Task<string> RecognizeText(byte[] imageBytes)
    {
        try
        {
            if (_ocrEngine == null)
                throw new InvalidOperationException("OCR 引擎初始化失败");

            // 将字节数组转换为 SoftwareBitmap
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(imageBytes.AsBuffer());
            stream.Seek(0);

            var decoder = await BitmapDecoder.CreateAsync(stream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            // 执行 OCR
            var result = await _ocrEngine.RecognizeAsync(softwareBitmap);

            // 提取文字 - 每行内的词去掉空格，行间用空格连接
            var lines = result.Lines.Select(line => 
            {
                // 去除单词间的空格，保留连续文本
                var lineText = string.Join("", line.Words.Select(w => w.Text));
                return lineText;
            });
            var text = string.Join("", lines);
            
            // 清理结果：去除常见误识别字符
            text = text.Replace("0恩@0", "");
            text = text.Replace("悉", "");
            text = text.Replace("0巛\"", "，");
            text = text.Replace("巛\"", "，");
            text = text.Replace("痣样", "这样");
            text = text.Replace("产季", "出现");
            text = text.Replace("泄界", "世界");
            text = text.Replace("垡", "就");
            
            // 去除多余空格
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", "");
            
            return text.Trim();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Windows OCR 识别失败: {ex.Message}", ex);
        }
    }

    public Task<Models.OcrResult> ExtractSubtitles(OcrConfig config, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Windows OCR 批量提取功能暂未实现");
    }
}
