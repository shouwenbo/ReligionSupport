using System.Text;
using System.Text.Json;

namespace WeChatPublisher.Services;

public class AIImageService
{
    private readonly HttpClient _httpClient;

    public AIImageService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
    }

    public async Task<byte[]> GenerateImageAsync(string prompt,
        string? provider = null, string? baseUrl = null,
        string? model = null, string? apiKey = null,
        string? size = null, int count = 1,
        CancellationToken ct = default)
    {
        var config = AppSettings.Instance.GetActiveImageAiConfig();
        provider ??= config?.ProviderName ?? "TokenHub";
        baseUrl ??= config?.BaseUrl ?? "https://tokenhub.tencentmaas.com/v1";
        model ??= config?.ModelName ?? "ep-km3k66ay";
        size ??= config?.ImageSize ?? "1024x1024";
        var imageCount = count < 1 ? 1 : count;

        if (apiKey == null && config?.ApiKeyEncrypted != null)
            apiKey = ConfigEncryptionService.Decrypt(config.ApiKeyEncrypted);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("未配置图像API Key");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        int logoAdd = config?.LogoAdd ?? 0;
        return provider switch
        {
            "TokenHub" => await GenerateViaTokenHub(baseUrl, model, prompt, size, imageCount, ct),
            "HunyuanImage" => await GenerateViaDalle(baseUrl, model, prompt, size, imageCount, logoAdd, ct),
            "DALLE" => await GenerateViaDalle(baseUrl, model, prompt, size, imageCount, logoAdd, ct),
            _ => await GenerateViaDalle(baseUrl, model, prompt, size, imageCount, logoAdd, ct)
        };
    }

    private async Task<byte[]> GenerateViaTokenHub(string baseUrl, string model,
        string prompt, string size, int count, CancellationToken ct)
    {
        var body = new
        {
            model,
            instructions = "You are an image generator. Generate the described image.",
            input = prompt,
            stream = false
        };

        var content = new StringContent(JsonSerializer.Serialize(body),
            Encoding.UTF8, "application/json");
        var resp = await _httpClient.PostAsync($"{baseUrl}/responses", content, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"TokenHub API 错误: {resp.StatusCode} - {json}");

        using var doc = JsonDocument.Parse(json);

        // Try to extract image URL or base64 from response
        if (doc.RootElement.TryGetProperty("output", out var output))
        {
            var outputText = output.GetString() ?? "";
            // Check if output contains an image URL
            if (outputText.StartsWith("http") && (outputText.Contains(".png") || outputText.Contains(".jpg")))
                return await _httpClient.GetByteArrayAsync(outputText, ct);

            // Check if output contains base64 image data
            if (outputText.Contains("base64,") || outputText.StartsWith("data:image"))
            {
                var b64 = outputText.Contains("base64,")
                    ? outputText.Split("base64,")[1].Trim()
                    : outputText;
                return Convert.FromBase64String(b64);
            }

            // Check for url field
            if (doc.RootElement.TryGetProperty("url", out var url))
                return await _httpClient.GetByteArrayAsync(url.GetString()!, ct);
        }

        throw new InvalidOperationException($"TokenHub 未返回图像数据: {json[..Math.Min(200, json.Length)]}");
    }

    private async Task<byte[]> GenerateViaDalle(string baseUrl, string model,
        string prompt, string size, int count, int logoAdd, CancellationToken ct)
    {
        var body = new
        {
            model,
            prompt,
            n = count,
            size,
            extra_body = new { logo_add = logoAdd }
        };

        var content = new StringContent(JsonSerializer.Serialize(body),
            Encoding.UTF8, "application/json");
        var resp = await _httpClient.PostAsync($"{baseUrl}/images/generations", content, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"TokenHub {resp.StatusCode}\n" +
                $"端点: {baseUrl}/images/generations\n" +
                $"模型: {model}\n" +
                $"尺寸: {size}\n" +
                $"错误: {json}");

        using var doc = JsonDocument.Parse(json);
        var url = doc.RootElement.GetProperty("data")[0].GetProperty("url").GetString()!;
        return await _httpClient.GetByteArrayAsync(url, ct);
    }
}
