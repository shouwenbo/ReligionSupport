using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AutoVideoClipper.WPF
{
    public class TtsService
    {
        private readonly HttpClient _client;
        private readonly CookieContainer _cookieContainer;
        private readonly string _baseUrl = "https://www.text-to-speech.cn/";
        private readonly Action<string> _log;

        public TtsService(Action<string> log)
        {
            _cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            _client = new HttpClient(handler);
            _client.DefaultRequestHeaders.Add("accept", "*/*");
            _client.DefaultRequestHeaders.Add("accept-language", "zh-CN,zh;q=0.9");
            _client.DefaultRequestHeaders.Add("x-requested-with", "XMLHttpRequest");
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36");
            _log = log;
        }

        /// <summary>
        /// 获取最新 token 并保存 cookie
        /// </summary>
        public async Task<string?> GetTokenAsync()
        {
            try
            {
                _log("🌐 访问首页获取 token + cookie...");
                var html = await _client.GetStringAsync(_baseUrl);
                var match = Regex.Match(html, @"const\s+token\s*=\s*'([a-z0-9]+)';", RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    string token = match.Groups[1].Value;
                    _log($"✅ 成功获取 token: {token}");
                    return token;
                }

                _log("⚠ 未找到 token，可能网页结构已变化！");
                return null;
            }
            catch (Exception ex)
            {
                _log($"❌ 获取 token 异常：{ex}");
                return null;
            }
        }

        /// <summary>
        /// 生成音频并下载
        /// </summary>
        public async Task<string?> GenerateAudioAsync(string text, string outputFile)
        {
            string? token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                _log("❌ 无法生成音频：token 为空！");
                return null;
            }

            var data = new Dictionary<string, string>
            {
                ["language"] = "中文（普通话，简体）",
                ["voice"] = "zh-CN-YunzeNeural",
                ["text"] = text,
                ["role"] = "OlderAdultMale",
                ["style"] = "calm",
                ["rate"] = "-26",
                ["pitch"] = "-10",
                ["kbitrate"] = "audio-48khz-192kbitrate-mono-mp3",
                ["silence"] = "500ms",
                ["styledegree"] = "2",
                ["volume"] = "x-loud",
                ["predict"] = "0",
                ["user_id"] = "",
                ["yzm"] = "202410170001",
                ["replice"] = "1",
                ["token"] = token
            };

            try
            {
                _log("📤 发送文本转语音请求...");
                var content = new FormUrlEncodedContent(data);
                var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + "getSpeek.php")
                {
                    Content = content
                };
                request.Headers.Referrer = new Uri(_baseUrl);

                var response = await _client.SendAsync(request);
                string json = await response.Content.ReadAsStringAsync();

                _log($"🔍 响应状态：{(int)response.StatusCode} {response.ReasonPhrase}");
                _log($"📄 原始响应：{json}");

                if (!response.IsSuccessStatusCode)
                {
                    _log("❌ 请求失败，可能被限流或网站拒绝访问！");
                    return null;
                }

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("code", out var code) && code.GetInt32() == 200)
                {
                    string downloadUrl = root.GetProperty("download").GetString()!;
                    _log($"✅ 音频生成成功，下载地址：{downloadUrl}");

                    // 下载音频
                    var bytes = await _client.GetByteArrayAsync(downloadUrl);
                    await File.WriteAllBytesAsync(outputFile, bytes);
                    _log($"✅ 音频下载完成: {outputFile}");
                    return outputFile;
                }
                else
                {
                    string msg = root.TryGetProperty("msg", out var msgProp) ? msgProp.GetString() ?? "未知错误" : "未返回 msg";
                    _log($"⚠ 音频生成失败，错误信息：{msg}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _log($"❌ 异常：{ex}");
                return null;
            }
        }
    }
}
