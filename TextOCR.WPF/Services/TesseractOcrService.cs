using System.IO;
using System.Text;
using Tesseract;
using TextOCR.WPF.Models;

namespace TextOCR.WPF.Services;

public class TesseractOcrService : IOcrService
{
    private readonly string _tessDataPath;

    public TesseractOcrService()
    {
        // Tesseract 数据文件路径
        _tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
        
        // 如果不存在，尝试从程序目录查找
        if (!Directory.Exists(_tessDataPath))
        {
            _tessDataPath = "./tessdata";
        }
    }

    public async Task<string> RecognizeText(byte[] imageBytes)
    {
        return await Task.Run(() =>
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Tesseract] 开始识别，图片大小: {imageBytes.Length} 字节");
                System.Diagnostics.Debug.WriteLine($"[Tesseract] 数据目录: {_tessDataPath}");
                
                // 检查 tessdata 目录是否存在
                if (!Directory.Exists(_tessDataPath))
                {
                    var errorMsg = $"Tesseract 数据目录不存在: {_tessDataPath}\n" +
                        "请下载中文语言包 chi_sim.traineddata 并放置到 tessdata 目录";
                    System.Diagnostics.Debug.WriteLine($"[Tesseract] 错误: {errorMsg}");
                    throw new InvalidOperationException(errorMsg);
                }

                // 检查语言文件是否存在
                var chiFile = Path.Combine(_tessDataPath, "chi_sim.traineddata");
                
                if (!File.Exists(chiFile))
                {
                    var errorMsg = $"缺少中文语言包: {chiFile}\n" +
                        "请下载最新中文简体语言包:\n" +
                        "1. 访问: https://github.com/tesseract-ocr/tessdata_best\n" +
                        "2. 下载 chi_sim.traineddata (适合高精度识别)\n" +
                        "3. 或访问: https://github.com/tesseract-ocr/tessdata_fast (更快但精度稍低)\n" +
                        $"4. 将文件放到: {_tessDataPath}";
                    System.Diagnostics.Debug.WriteLine($"[Tesseract] 错误: {errorMsg}");
                    throw new InvalidOperationException(errorMsg);
                }
                
                System.Diagnostics.Debug.WriteLine($"[Tesseract] 语言文件检查通过");

                // 只使用中文语言包
                using var engine = new TesseractEngine(_tessDataPath, "chi_sim", EngineMode.Default);
                
                // 设置识别变量以提高准确率
                engine.SetVariable("tessedit_char_whitelist", "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ一二三四五六七八九十百千万亿上中下大小多少前后左右东南西北今天明年月日时分秒");
                engine.SetVariable("language_model_penalty_non_dict_word", "0.8");
                engine.SetVariable("language_model_penalty_non_freq_dict_word", "0.8");
                
                // 加载图片
                using var img = Pix.LoadFromMemory(imageBytes);
                
                System.Diagnostics.Debug.WriteLine($"[Tesseract] 原始图片尺寸: {img.Width}x{img.Height}");
                
                // 图像预处理：转灰度图
                using var gray = img.ConvertRGBToGray();
                
                // 使用Otsu二值化提高对比度
                using var binary = gray.BinarizeOtsuAdaptiveThreshold(2000, 2000, 0, 0, 0.1f);
                
                System.Diagnostics.Debug.WriteLine($"[Tesseract] 预处理完成");
                
                // 字幕通常是单行文本，直接使用 PSM 7
                engine.DefaultPageSegMode = PageSegMode.SingleLine;
                
                string bestResult = string.Empty;
                float bestConfidence = 0;
                
                try
                {
                    using var page = engine.Process(binary);
                    bestResult = page.GetText();
                    bestConfidence = page.GetMeanConfidence();
                    
                    System.Diagnostics.Debug.WriteLine($"[Tesseract] 识别结果 (置信度={bestConfidence:F2}): {bestResult?.Trim()}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Tesseract] 识别失败: {ex.Message}");
                }
                
                // 清理结果：移除换行符和多余空格
                if (!string.IsNullOrEmpty(bestResult))
                {
                    bestResult = bestResult.Replace("\n", "").Replace("\r", "");
                    bestResult = System.Text.RegularExpressions.Regex.Replace(bestResult, @"\s+", "");
                }
                
                System.Diagnostics.Debug.WriteLine($"[Tesseract] 最终结果: {bestResult}");
                
                return bestResult?.Trim() ?? string.Empty;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Tesseract OCR 识别失败: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[Tesseract] 异常: {errorMsg}");
                System.Diagnostics.Debug.WriteLine($"[Tesseract] 堆栈: {ex.StackTrace}");
                throw new InvalidOperationException(errorMsg, ex);
            }
        });
    }

    public async Task<OcrResult> ExtractSubtitles(OcrConfig config, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        // Tesseract 批量提取实现
        // 这里可以调用 Python 脚本或者自己实现
        throw new NotImplementedException("Tesseract 批量提取功能暂未实现，请使用 PaddleOCR");
    }
}
