using System.Diagnostics;
using System.IO;
using System.Text;
using TextOCR.WPF.Models;

namespace TextOCR.WPF.Services;

public interface IOcrService
{
    Task<string> RecognizeText(byte[] imageBytes);
    Task<OcrResult> ExtractSubtitles(OcrConfig config, IProgress<int>? progress = null, CancellationToken cancellationToken = default);
}

public class PythonOcrService : IOcrService
{
    private readonly string _pythonScriptPath;
    private readonly string _pythonExePath;
    private string _initializationError = string.Empty;

    // 静态事件用于输出日志到主界面
    public static event Action<string>? LogMessage;

    public PythonOcrService()
    {
        _pythonScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PythonScripts");
        
        // 尝试查找 Python 可执行文件
        _pythonExePath = FindPythonExecutable() ?? "python";
    }

    private void Log(string message)
    {
        System.Diagnostics.Debug.WriteLine(message);
        LogMessage?.Invoke(message);
    }

    private string? FindPythonExecutable()
    {
        var possiblePaths = new[]
        {
            @"C:\Users\寿文博\AppData\Local\Programs\Python\Python313\python.exe",
            @"C:\Python313\python.exe",
            @"C:\Python312\python.exe",
            @"C:\Python311\python.exe",
            @"C:\Python310\python.exe"
        };

        return possiblePaths.FirstOrDefault(File.Exists);
    }

    public async Task<string> RecognizeText(byte[] imageBytes)
    {
        // 检查初始化状态
        if (!string.IsNullOrEmpty(_initializationError))
        {
            Log($"[PaddleOCR] 警告: {_initializationError}");
        }
        
        // 使用英文路径作为临时目录，避免中文用户名问题
        var tempDir = @"C:\PaddleOCR_Models\temp";
        Directory.CreateDirectory(tempDir);
        
        var tempImagePath = Path.Combine(tempDir, $"ocr_temp_{Guid.NewGuid()}.png");

        try
        {
            Log($"[PaddleOCR] 开始识别，图片大小: {imageBytes.Length} 字节");
            
            await File.WriteAllBytesAsync(tempImagePath, imageBytes);

            var scriptPath = Path.Combine(_pythonScriptPath, "ocr_simple.py");
            
            if (!File.Exists(scriptPath))
            {
                throw new InvalidOperationException($"找不到 Python 脚本: {scriptPath}");
            }

            var arguments = $"\"{scriptPath}\" \"{tempImagePath}\"";

            var psi = new ProcessStartInfo
            {
                FileName = _pythonExePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            
            // 设置环境变量
            psi.Environment["PYTHONIOENCODING"] = "utf-8";
            psi.Environment["PYTHONUTF8"] = "1";  // Python 3.7+ 强制UTF-8模式
            psi.Environment["PADDLEX_HOME"] = "C:/PaddleOCR_Models";
            psi.Environment["HOME"] = "C:/PaddleOCR_Models";
            psi.Environment["USERPROFILE"] = "C:/PaddleOCR_Models";
            psi.Environment["PADDLE_HOME"] = "C:/PaddleOCR_Models";

            using var process = Process.Start(psi);
            if (process == null)
                throw new InvalidOperationException("无法启动 Python 进程");

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(error))
            {
                // 过滤掉不影响结果的警告信息
                var filteredError = FilterWarnings(error);
                if (!string.IsNullOrWhiteSpace(filteredError))
                {
                    Log($"[PaddleOCR] 错误: {filteredError}");
                }
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"PaddleOCR 识别失败，退出代码: {process.ExitCode}\n错误信息: {error}");
            }

            var result = output?.Trim() ?? string.Empty;
            
            return result;
        }
        catch (Exception ex)
        {
            Log($"[PaddleOCR] 异常: {ex.Message}");
            throw;
        }
        finally
        {
            TryDeleteFile(tempImagePath);
        }
    }

    public async Task<OcrResult> ExtractSubtitles(OcrConfig config, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;

        try
        {
            var scriptPath = Path.Combine(_pythonScriptPath, "extract_subtitles.py");
            
            if (!File.Exists(scriptPath))
            {
                return new OcrResult
                {
                    Success = false,
                    ErrorMessage = $"找不到 Python 脚本: {scriptPath}"
                };
            }
            
            var arguments = BuildPythonArguments(scriptPath, config);

            var psi = new ProcessStartInfo
            {
                FileName = _pythonExePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            
            // 设置环境变量以避免中文路径问题
            psi.Environment["PYTHONIOENCODING"] = "utf-8";
            psi.Environment["PYTHONUTF8"] = "1";  // Python 3.7+ 强制UTF-8模式
            psi.Environment["PADDLEX_HOME"] = "C:/PaddleOCR_Models";
            psi.Environment["HOME"] = "C:/PaddleOCR_Models";
            psi.Environment["USERPROFILE"] = "C:/PaddleOCR_Models";
            psi.Environment["PADDLE_HOME"] = "C:/PaddleOCR_Models";

            using var process = Process.Start(psi);
            if (process == null)
                throw new InvalidOperationException("无法启动 Python 进程");

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    outputBuilder.AppendLine(args.Data);
                    
                    // 过滤无关的警告信息，只显示有用的日志
                    var filteredData = FilterWarnings(args.Data);
                    if (!string.IsNullOrWhiteSpace(filteredData))
                    {
                        Log($"[提取] {filteredData}");
                    }
                    
                    // 解析进度信息：格式 "处理帧: 10/100 (10.0%) - 已识别字幕: 5 条"
                    if (args.Data.Contains("处理帧:") && progress != null)
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(args.Data, @"处理帧:\s*(\d+)/(\d+)\s*\((\d+\.?\d*)%\)");
                        if (match.Success && double.TryParse(match.Groups[3].Value, out var percentage))
                        {
                            progress.Report((int)percentage);
                        }
                    }
                }
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    errorBuilder.AppendLine(args.Data);
                    
                    // 过滤错误输出中的无关警告
                    var filteredError = FilterWarnings(args.Data);
                    if (!string.IsNullOrWhiteSpace(filteredError))
                    {
                        Log($"[提取错误] {filteredError}");
                    }
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            var outputText = outputBuilder.ToString();
            var errorText = errorBuilder.ToString();

            if (process.ExitCode != 0)
            {
                var errorDetail = $"退出代码: {process.ExitCode}\n{errorText}";
                Log($"[提取] 失败: {errorDetail}");
                
                return new OcrResult
                {
                    Success = false,
                    ErrorMessage = errorDetail
                };
            }

            return new OcrResult
            {
                Success = true,
                OutputFilePath = config.OutputPath,
                ProcessingTime = DateTime.Now - startTime
            };
        }
        catch (Exception ex)
        {
            Log($"[提取] 异常: {ex.Message}");
            
            return new OcrResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingTime = DateTime.Now - startTime
            };
        }
    }

    private string BuildPythonArguments(string scriptPath, OcrConfig config)
    {
        var args = new List<string>
        {
            $"\"{scriptPath}\"",
            $"\"{config.VideoPath}\"",
            $"--subtitle_height {config.SubtitleHeight}",
            $"--frame_interval {config.FrameInterval.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            $"--from_top {(config.FromTop ? "True" : "False")}",
            $"--output_file \"{config.OutputPath}\""
        };

        if (!string.IsNullOrWhiteSpace(config.TimeRange))
        {
            args.Add($"--time_range \"{config.TimeRange}\"");
        }

        return string.Join(" ", args);
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Ignore
        }
    }

    private string FilterWarnings(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            return string.Empty;

        var lines = error.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var filteredLines = new List<string>();

        foreach (var line in lines)
        {
            // 过滤掉不影响结果的警告
            if (line.Contains("Checking connectivity") ||
                line.Contains("DISABLE_MODEL_SOURCE_CHECK") ||
                line.Contains("ccache") ||
                line.Contains("Creating model:") ||
                line.Contains("Using official model") ||
                line.Contains("Model files already exist") ||
                line.Contains("Fetching") ||
                line.Contains("[32m") ||  // 绿色文本
                line.Contains("[33m") ||  // 黄色文本
                line.Contains("[0m") ||   // 重置颜色
                line.Contains("信息:") ||
                line.Contains("所提供的模式无法找到文件"))
            {
                continue;
            }
            filteredLines.Add(line);
        }

        return string.Join(Environment.NewLine, filteredLines);
    }
}
