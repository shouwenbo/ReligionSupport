using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WeChatPublisher.Services;

public class SubtitleService
{
    private readonly HttpClient _httpClient;
    private readonly Models.TtsApiConfig _config;
    private readonly Action<string>? _log;

    public SubtitleService(Models.TtsApiConfig config, Action<string>? log = null)
    {
        _httpClient = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            CookieContainer = new CookieContainer()
        })
        { Timeout = TimeSpan.FromMinutes(10) };
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        _config = config;
        _log = log;
    }

    public async Task<string> GenerateSubtitleAsync(string audioFilePath, string language = "zh-CN")
    {
        // Step 1: Get token from page
        _log?.Invoke("正在获取字幕Token...");
        var tokenUrl = _config.BaseUrl + _config.TokenFetchUrl;
        var html = await _httpClient.GetStringAsync(tokenUrl);
        var tokenMatch = Regex.Match(html, _config.TokenRegexPattern ?? @"const\s+token\s*=\s*'([a-f0-9]{32})'");
        if (!tokenMatch.Success)
            throw new InvalidOperationException("未找到字幕Token，请检查网页结构是否更新");
        var token = tokenMatch.Groups[1].Value;

        // Step 2: Upload audio to get subtitles
        _log?.Invoke("正在上传音频生成字幕...");
        var url = _config.BaseUrl + _config.GenerateEndpoint;
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(language), "language");
        form.Add(new StringContent(token), "token");
        form.Add(new StreamContent(File.OpenRead(audioFilePath)), "video", Path.GetFileName(audioFilePath));

        var response = await _httpClient.PostAsync(url, form);
        var result = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;

        var codeField = _config.SuccessCodeField;
        if (!root.TryGetProperty(codeField, out var codeProp))
            throw new InvalidOperationException("返回结果中缺少code字段");
        if (codeProp.GetInt32() != int.Parse(_config.SuccessCodeValue))
        {
            var msg = root.TryGetProperty("msg", out var m) ? m.GetString() : "未知错误";
            throw new InvalidOperationException($"字幕接口返回失败: {msg}");
        }

        var subtitleUrl = root.GetProperty("download").GetString() ?? "";
        if (string.IsNullOrWhiteSpace(subtitleUrl))
            throw new InvalidOperationException("返回的下载地址为空");

        // Handle relative URLs
        if (subtitleUrl.StartsWith("/"))
            subtitleUrl = _config.BaseUrl + subtitleUrl;
        else if (!subtitleUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            subtitleUrl = _config.BaseUrl + "/" + subtitleUrl;

        // Step 3: Download SRT file
        _log?.Invoke($"正在下载字幕: {subtitleUrl}");
        var bytes = await _httpClient.GetByteArrayAsync(subtitleUrl);
        var outputPath = Path.Combine(
            Path.GetDirectoryName(audioFilePath)!,
            Path.GetFileNameWithoutExtension(audioFilePath) + ".srt");
        await File.WriteAllBytesAsync(outputPath, bytes);
        _log?.Invoke($"字幕已保存: {outputPath}");
        return outputPath;
    }
}
