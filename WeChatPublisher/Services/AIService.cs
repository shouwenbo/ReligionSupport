using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace WeChatPublisher.Services;

public interface ILLMClient
{
    Task<string> ChatAsync(string systemPrompt, string userMessage,
        double temperature = 0.7, int maxTokens = 4096, CancellationToken ct = default);
    IAsyncEnumerable<string> ChatStreamAsync(string systemPrompt, string userMessage,
        double temperature = 0.7, int maxTokens = 4096, CancellationToken ct = default);
}

public class DeepSeekClient : ILLMClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _baseUrl;

    public DeepSeekClient(HttpClient httpClient, string apiKey,
        string baseUrl = "https://api.deepseek.com", string model = "deepseek-chat")
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _baseUrl = baseUrl.TrimEnd('/');
        _model = model;
    }

    public async Task<string> ChatAsync(string systemPrompt, string userMessage,
        double temperature = 0.7, int maxTokens = 4096, CancellationToken ct = default)
    {
        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userMessage }
        };

        var body = new
        {
            model = _model,
            messages,
            temperature,
            max_tokens = maxTokens,
            stream = false
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
        return content ?? "";
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(string systemPrompt, string userMessage,
        double temperature = 0.7, int maxTokens = 4096,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userMessage }
        };

        var body = new
        {
            model = _model,
            messages,
            temperature,
            max_tokens = maxTokens,
            stream = true
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;
            var data = line[6..];
            if (data == "[DONE]") break;

            string? content = null;
            try
            {
                using var doc = JsonDocument.Parse(data);
                var delta = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("delta");
                if (delta.TryGetProperty("content", out var c))
                    content = c.GetString();
            }
            catch { }

            if (content != null)
                yield return content;
        }
    }
}

public class AIService
{
    private ILLMClient? _textClient;
    private readonly SensitiveWordService _sensitiveWord;
    private readonly AppSettings _settings;

    public AIService(SensitiveWordService sensitiveWord)
    {
        _sensitiveWord = sensitiveWord;
        _settings = AppSettings.Instance;
    }

    public ILLMClient? TextClient => _textClient;

    public void ConfigureFromSettings()
    {
        var config = _settings.GetActiveTextAiConfig();
        if (config == null) return;

        var apiKey = config.ApiKeyEncrypted != null
            ? ConfigEncryptionService.Decrypt(config.ApiKeyEncrypted)
            : "";
        var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };

        _textClient = config.ProviderName.ToLower() switch
        {
            "deepseek" => new DeepSeekClient(httpClient, apiKey, config.BaseUrl, config.ModelName),
            "openai" => new DeepSeekClient(httpClient, apiKey, config.BaseUrl, config.ModelName),
            _ => new DeepSeekClient(httpClient, apiKey, config.BaseUrl, config.ModelName)
        };
    }

    public Task<string> GenerateTextAsync(string systemPrompt, string userMessage,
        double? temperature = null, CancellationToken ct = default)
    {
        if (_textClient == null) ConfigureFromSettings();
        if (_textClient == null) throw new InvalidOperationException("未配置AI文本模型");

        return _textClient.ChatAsync(systemPrompt, userMessage,
            temperature ?? _settings.DefaultTemperature, ct: ct);
    }

    public IAsyncEnumerable<string> GenerateTextStreamAsync(string systemPrompt, string userMessage,
        double? temperature = null, CancellationToken ct = default)
    {
        if (_textClient == null) ConfigureFromSettings();
        if (_textClient == null) throw new InvalidOperationException("未配置AI文本模型");

        return _textClient.ChatStreamAsync(systemPrompt, userMessage,
            temperature ?? _settings.DefaultTemperature, ct: ct);
    }
}
