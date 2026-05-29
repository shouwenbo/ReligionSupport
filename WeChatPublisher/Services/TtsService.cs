using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WeChatPublisher.Services;

public class TtsService
{
    private readonly HttpClient _httpClient;
    private readonly Models.TtsApiConfig _config;
    private string? _cachedToken;

    public TtsService(Models.TtsApiConfig config)
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
        _cachedToken = match.Success ? match.Groups[1].Value : null;
        return _cachedToken;
    }

    public async Task<string?> GenerateAudioAsync(string text, string outputFilePath,
        Dictionary<string, string>? overrideParams = null)
    {
        if (_cachedToken == null)
        {
            var token = await AcquireTokenAsync();
            if (token == null) return null;
        }

        var defaultParams = new Dictionary<string, string>();
        if (_config.DefaultParamsJSON != null)
        {
            try
            {
                defaultParams = JsonSerializer.Deserialize<Dictionary<string, string>>(_config.DefaultParamsJSON)
                    ?? new Dictionary<string, string>();
            }
            catch { }
        }

        var formData = new Dictionary<string, string>(defaultParams)
        {
            ["text"] = text,
            ["token"] = _cachedToken!
        };

        if (overrideParams != null)
        {
            foreach (var kv in overrideParams)
                formData[kv.Key] = kv.Value;
        }

        var url = _config.BaseUrl + _config.GenerateEndpoint;
        var content = new FormUrlEncodedContent(formData);
        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var codeField = _config.SuccessCodeField;
        var codeValue = _config.SuccessCodeValue;
        if (root.TryGetProperty(codeField, out var codeEl) && codeEl.GetRawText() != codeValue)
            return null;

        string? downloadUrl = null;
        if (root.TryGetProperty("download", out var dl))
            downloadUrl = dl.GetString();
        if (downloadUrl == null && root.TryGetProperty("url", out var u))
            downloadUrl = u.GetString();
        if (downloadUrl == null) return null;

        var audioBytes = await _httpClient.GetByteArrayAsync(downloadUrl);
        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath)!);
        await File.WriteAllBytesAsync(outputFilePath, audioBytes);
        return outputFilePath;
    }
}
