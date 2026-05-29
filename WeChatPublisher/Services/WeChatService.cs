using System.Net.Http;
using System.Text.Json;

namespace WeChatPublisher.Services;

public class WeChatService
{
    private readonly HttpClient _httpClient;
    private readonly AppSettings _settings;
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public WeChatService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _settings = AppSettings.Instance;
    }

    public async Task<string> GetAccessTokenAsync(Models.WeChatConfig? account = null, bool forceRefresh = false)
    {
        if (!forceRefresh && _accessToken != null && DateTime.Now < _tokenExpiry)
            return _accessToken;

        var config = account ?? _settings.GetActiveWeChatConfig();
        if (config == null) throw new InvalidOperationException("未配置微信公众号");

        var appSecret = ConfigEncryptionService.Decrypt(config.AppSecretEncrypted!);
        var url = $"{config.ApiBaseUrl}/cgi-bin/token?grant_type=client_credential&appid={config.AppId}&secret={appSecret}";

        var response = await _httpClient.GetStringAsync(url);
        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        if (root.TryGetProperty("access_token", out var token))
        {
            _accessToken = token.GetString();
            var expiresIn = root.GetProperty("expires_in").GetInt32();
            _tokenExpiry = DateTime.Now.AddSeconds(expiresIn - 300);
            return _accessToken!;
        }

        var errmsg = root.TryGetProperty("errmsg", out var em) ? em.GetString() : "未知错误";
        throw new InvalidOperationException($"获取AccessToken失败: {errmsg}");
    }

    public async Task<string> CreateDraftAsync(Models.ArticleDraft draft)
    {
        var token = await GetAccessTokenAsync();
        var url = $"{_settings.GetActiveWeChatConfig()!.ApiBaseUrl}/cgi-bin/draft/add?access_token={token}";

        var articles = new[]
        {
            new
            {
                title = draft.Title,
                author = "爱与祝福同行",
                digest = draft.Description ?? "",
                content = draft.Content ?? "",
                content_source_url = "",
                need_open_comment = 0,
                only_fans_can_comment = 0
            }
        };

        var body = new { articles };
        var content = new StringContent(JsonSerializer.Serialize(body),
            System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("media_id", out var mediaId))
            return mediaId.GetString() ?? "";

        var errmsg = root.TryGetProperty("errmsg", out var em) ? em.GetString() : "未知错误";
        throw new InvalidOperationException($"创建草稿失败: {errmsg}");
    }

    public async Task<string> UploadImageAsync(string imageFilePath)
    {
        var token = await GetAccessTokenAsync();
        var url = $"{_settings.GetActiveWeChatConfig()!.ApiBaseUrl}/cgi-bin/material/add_material?access_token={token}&type=image";

        using var formData = new MultipartFormDataContent();
        var fileBytes = await File.ReadAllBytesAsync(imageFilePath);
        formData.Add(new ByteArrayContent(fileBytes), "media", Path.GetFileName(imageFilePath));

        var response = await _httpClient.PostAsync(url, formData);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("url", out var imgUrl))
            return imgUrl.GetString() ?? "";

        throw new InvalidOperationException("上传图片失败");
    }

    public async Task<string> PublishAsync(string mediaId)
    {
        var token = await GetAccessTokenAsync();
        var url = $"{_settings.GetActiveWeChatConfig()!.ApiBaseUrl}/cgi-bin/freepublish/submit?access_token={token}";

        var body = new { media_id = mediaId };
        var content = new StringContent(JsonSerializer.Serialize(body),
            System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("publish_id", out var publishId))
            return publishId.GetString() ?? "";

        var errmsg = root.TryGetProperty("errmsg", out var em) ? em.GetString() : "未知错误";
        throw new InvalidOperationException($"发布失败: {errmsg}");
    }
}
