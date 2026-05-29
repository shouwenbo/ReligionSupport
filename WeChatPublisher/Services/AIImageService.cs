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
        CancellationToken ct = default)
    {
        var config = AppSettings.Instance.GetActiveImageAiConfig();
        provider ??= config?.ProviderName ?? "TokenHub";
        baseUrl ??= config?.BaseUrl ?? "https://tokenhub.tencentmaas.com/v1";
        model ??= config?.ModelName ?? "ep-km3k66ay";

        if (apiKey == null && config?.ApiKeyEncrypted != null)
            apiKey = ConfigEncryptionService.Decrypt(config.ApiKeyEncrypted);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("未配置图像API Key");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        return provider switch
        {
            "TokenHub" => await GenerateViaTokenHub(baseUrl, model, prompt, ct),
            "HunyuanImage" => await GenerateViaDalle(baseUrl, model, prompt, ct),
            "DALLE" => await GenerateViaDalle(baseUrl, model, prompt, ct),
            _ => await GenerateViaDalle(baseUrl, model, prompt, ct)
        };
    }

    private async Task<byte[]> GenerateViaTokenHub(string baseUrl, string model,
        string prompt, CancellationToken ct)
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
        string prompt, CancellationToken ct)
    {
        var body = new
        {
            model,
            prompt,
            n = 1,
            size = "1024x1024"
        };

        var content = new StringContent(JsonSerializer.Serialize(body),
            Encoding.UTF8, "application/json");
        var resp = await _httpClient.PostAsync($"{baseUrl}/images/generations", content, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"图像API 错误: {resp.StatusCode} - {json}");

        using var doc = JsonDocument.Parse(json);
        var url = doc.RootElement.GetProperty("data")[0].GetProperty("url").GetString()!;
        return await _httpClient.GetByteArrayAsync(url, ct);
    }
}
