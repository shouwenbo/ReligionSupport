using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WeChatPublisher.Services;

public class SubtitleService
{
    private readonly HttpClient _httpClient;
    private readonly Models.TtsApiConfig _config;

    public SubtitleService(Models.TtsApiConfig config)
    {
        _httpClient = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            CookieContainer = new CookieContainer()
        });
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        _config = config;
    }

    public async Task<string?> AcquireTokenAsync()
    {
        if (_config.TokenFetchUrl == null || _config.TokenRegexPattern == null)
            return null;

        var url = _config.BaseUrl + _config.TokenFetchUrl;
        var html = await _httpClient.GetStringAsync(url);
        var match = Regex.Match(html, _config.TokenRegexPattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    public async Task<string> GenerateSubtitleAsync(string audioFilePath, string language = "zh-CN")
    {
        var token = await AcquireTokenAsync();
        if (token == null) throw new InvalidOperationException("无法获取字幕服务Token");

        var url = _config.BaseUrl + _config.GenerateEndpoint;
        using var formData = new MultipartFormDataContent();
        var fileBytes = await File.ReadAllBytesAsync(audioFilePath);
        formData.Add(new ByteArrayContent(fileBytes), "file", Path.GetFileName(audioFilePath));
        formData.Add(new StringContent(language), "language");
        formData.Add(new StringContent(token), "token");

        var response = await _httpClient.PostAsync(url, formData);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string? downloadUrl = null;
        if (root.TryGetProperty("download", out var dl))
            downloadUrl = dl.GetString();
        if (downloadUrl == null && root.TryGetProperty("url", out var u))
            downloadUrl = u.GetString();
        if (downloadUrl == null) throw new InvalidOperationException("字幕生成返回数据异常");

        var srtContent = await _httpClient.GetStringAsync(downloadUrl);

        // Post-process: trim punctuation, split long lines
        var lines = srtContent.Split('\n');
        var result = new List<string>();
        foreach (var line in lines)
        {
            if (line.Contains("-->") || int.TryParse(line, out _) || string.IsNullOrWhiteSpace(line))
            {
                result.Add(line);
            }
            else
            {
                var trimmed = line.Trim().TrimEnd('.', ',', '!', '?', '，', '。', '！', '？');
                result.Add(trimmed.Length > 8 ? SplitLongLine(trimmed) : trimmed);
            }
        }

        var outputPath = Path.ChangeExtension(audioFilePath, ".srt");
        await File.WriteAllTextAsync(outputPath, string.Join("\n", result));
        return outputPath;
    }

    private static string SplitLongLine(string line)
    {
        if (line.Length <= 8) return line;
        int mid = line.Length / 2;
        return line[..mid] + "\n" + line[mid..];
    }
}
