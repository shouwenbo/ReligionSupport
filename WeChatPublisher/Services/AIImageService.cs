namespace WeChatPublisher.Services;

public class AIImageService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public AIImageService(string baseUrl, string apiKey)
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        _apiKey = apiKey;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<byte[]> GenerateImageAsync(string prompt,
        int width = 1024, int height = 1024, CancellationToken ct = default)
    {
        var body = new
        {
            model = "dall-e-3",
            prompt,
            n = 1,
            size = $"{width}x{height}"
        };

        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(body),
            System.Text.Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_baseUrl}/v1/images/generations")
        {
            Content = content
        };
        request.Headers.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var url = doc.RootElement
            .GetProperty("data")[0]
            .GetProperty("url")
            .GetString();

        if (url == null) throw new InvalidOperationException("图像生成返回数据异常");

        return await _httpClient.GetByteArrayAsync(url, ct);
    }
}
