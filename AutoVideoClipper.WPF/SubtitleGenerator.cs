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
        /// 自动获取网页 token 并生成字幕
        /// </summary>
        public async Task<string?> TryGenerateSubtitleAsync(string audioPath, string language = "zh-CN")
        {
            // Step 1️⃣ 获取网页 HTML
            string html = await _client.GetStringAsync("https://www.text-to-speech.cn/srt.html");

            // Step 2️⃣ 提取 token
            var tokenMatch = Regex.Match(html, @"const\s+token\s*=\s*'([a-f0-9]{32})'");
            if (!tokenMatch.Success)
            {
                _log("❌ 未找到 token，请检查网页结构是否更新。");
                return null;
            }

            string token = tokenMatch.Groups[1].Value;
            _log($"✅ 获取 token 成功: {token}");

            // Step 3️⃣ 上传音频文件生成字幕
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(language), "language");
            form.Add(new StringContent(token), "token");
            form.Add(new StreamContent(File.OpenRead(audioPath)), "video", Path.GetFileName(audioPath));

            var response = await _client.PostAsync("https://www.text-to-speech.cn/getSrt.php", form);
            string result = await response.Content.ReadAsStringAsync();

            try
            {
                using var doc = JsonDocument.Parse(result);
                var root = doc.RootElement;

                if (root.TryGetProperty("code", out var codeProp) && codeProp.GetInt32() == 200)
                {
                    string url = root.GetProperty("download").GetString()!;
                    _log($"✅ 字幕生成成功：{url}");
                    return url;
                }
                else
                {
                    _log($"⚠️ 生成失败：{root.GetProperty("msg").GetString()}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _log($"❌ 解析失败: {ex.Message}");
                _log(result);
                return null;
            }
        }
    }

}
