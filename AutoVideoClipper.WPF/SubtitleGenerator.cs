using System.Net.Http;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.IO;

namespace AutoClipper
{
    public class SubtitleGenerator
    {
        private readonly HttpClient _client = new()
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
        private readonly Action<string> _log;

        public SubtitleGenerator(Action<string> log)
        {
            _log = log;
        }

        /// <summary>
        /// 自动生成字幕（含获取 token、上传、下载全过程）
        /// </summary>
        public async Task<string> GenerateSubtitleAsync(string audioPath, string language = "zh-CN")
        {
            try
            {
                // Step 1️⃣ 获取网页 HTML
                _log("🌐 正在获取字幕生成页面...");
                string html = await _client.GetStringAsync("https://www.text-to-speech.cn/srt.html");

                // Step 2️⃣ 提取 token
                var tokenMatch = Regex.Match(html, @"const\s+token\s*=\s*'([a-f0-9]{32})'");
                if (!tokenMatch.Success)
                    throw new Exception("未找到 token，请检查网页结构是否更新。");

                string token = tokenMatch.Groups[1].Value;
                _log($"✅ 获取 token 成功: {token}");

                // Step 3️⃣ 上传音频生成字幕
                using var form = new MultipartFormDataContent();
                form.Add(new StringContent(language), "language");
                form.Add(new StringContent(token), "token");
                form.Add(new StreamContent(File.OpenRead(audioPath)), "video", Path.GetFileName(audioPath));

                _log("🎧 正在上传音频文件生成字幕...");
                var response = await _client.PostAsync("https://www.text-to-speech.cn/getSrt.php", form);
                string result = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(result);
                var root = doc.RootElement;

                if (!root.TryGetProperty("code", out var codeProp))
                    throw new Exception("返回结果中缺少 code 字段。");

                if (codeProp.GetInt32() != 200)
                {
                    string msg = root.TryGetProperty("msg", out var msgProp) ? msgProp.GetString() ?? "未知错误" : "未知错误";
                    throw new Exception($"接口返回失败：{msg}");
                }

                string subtitleUrl = root.GetProperty("download").GetString() ?? "";
                if (string.IsNullOrWhiteSpace(subtitleUrl))
                    throw new Exception("接口返回的下载地址为空。");

                if (subtitleUrl.StartsWith("/"))
                    subtitleUrl = "https://www.text-to-speech.cn" + subtitleUrl;
                else if (!subtitleUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    subtitleUrl = "https://www.text-to-speech.cn/" + subtitleUrl;

                _log("✅ 在线字幕生成成功：" + subtitleUrl);

                // Step 4️⃣ 下载字幕文件
                string tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp");
                Directory.CreateDirectory(tempDir);
                string srtFile = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(audioPath) + ".srt");

                _log("📥 正在下载字幕文件...");
                var bytes = await _client.GetByteArrayAsync(subtitleUrl);
                await File.WriteAllBytesAsync(srtFile, bytes);

                if (!File.Exists(srtFile))
                    throw new Exception("字幕文件下载后不存在！");

                _log($"✅ 字幕文件已下载完成: {srtFile}");
                return srtFile;
            }
            catch (Exception ex)
            {
                _log($"❌ 在线字幕生成失败: {ex.Message}");
                throw; // 保留异常往上抛，让上层中断程序
            }
        }
    }
}
