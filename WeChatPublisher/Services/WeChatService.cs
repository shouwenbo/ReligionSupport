using System.Net.Http;
using System.Text.Json;

namespace WeChatPublisher.Services;

public class WeChatService
{
    private readonly HttpClient _httpClient;
    private readonly AppSettings _settings;
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    private static void LogApi(string action, string detail, Exception? ex = null)
    {
        if (ex != null)
            Logger.Error($"微信API [{action}] {detail}\n{ex}");
        else
            Logger.Info($"微信API [{action}] {detail}");
    }

    public WeChatService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _settings = AppSettings.Instance;
    }

    private static readonly SemaphoreSlim _tokenLock = new(1, 1);

    public async Task<string> GetAccessTokenAsync(Models.WeChatConfig? account = null, bool forceRefresh = false)
    {
        if (!forceRefresh && _accessToken != null && DateTime.Now < _tokenExpiry)
            return _accessToken;

        await _tokenLock.WaitAsync();
        try
        {
            // 双重检查：等锁期间可能已被其他线程刷新
            if (!forceRefresh && _accessToken != null && DateTime.Now < _tokenExpiry)
                return _accessToken;

            var config = account ?? _settings.GetActiveWeChatConfig();
            if (config == null) throw new InvalidOperationException("未配置微信公众号");
            if (string.IsNullOrWhiteSpace(config.AppId) || string.IsNullOrWhiteSpace(config.AppSecretEncrypted))
                throw new InvalidOperationException("请先填写 AppID 和 AppSecret");
            var appSecret = config.AppSecretEncrypted ?? "";
            LogApi("获取Token", $"AppId={config.AppId}");

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
        LogApi("获取Token失败", $"响应: {response}");
        throw new InvalidOperationException($"获取AccessToken失败: {errmsg}");
        }
        finally { _tokenLock.Release(); }
    }

    public async Task<string> CreateDraftAsync(Models.ArticleDraft draft)
    {
        var token = await GetAccessTokenAsync();
        var baseUrl = _settings.GetActiveWeChatConfig()!.ApiBaseUrl;

        // 1. 上传AI生成的封面图片获取 thumb_media_id
        var coverPath = draft.ImagePaths;
        string? thumbMediaId = null;
        if (!string.IsNullOrWhiteSpace(coverPath) && File.Exists(coverPath))
        {
            try { thumbMediaId = await UploadImageAsync(token, coverPath); }
            catch (Exception ex) { LogApi("封面上传失败", ex.Message); }
        }

        // 2. 清理文章内容中的本地图片路径（替换为占位或删除）
        var content = draft.Content ?? "";
        content = System.Text.RegularExpressions.Regex.Replace(content,
            @"<img[^>]*src='[A-Za-z]:\\[^']*'[^>]*>", "<p style='text-align:center;color:#999;'>[配图]</p>");

        var url = $"{baseUrl}/cgi-bin/draft/add?access_token={token[..Math.Min(6, token.Length)]}...";

        var article = new
        {
            title = draft.Title ?? "无标题",
            author = "爱与祝福同行",
            digest = draft.Description ?? "",
            content,
            content_source_url = "",
            thumb_media_id = thumbMediaId ?? "",
            need_open_comment = 0,
            only_fans_can_comment = 0
        };

        var body = new { articles = new[] { article } };
        var httpContent = new StringContent(JsonSerializer.Serialize(body),
            System.Text.Encoding.UTF8, "application/json");

        LogApi("创建草稿", $"标题={draft.Title}, 内容长度={content.Length}, 封面={thumbMediaId ?? "无"}");
        var response = await _httpClient.PostAsync(url, httpContent);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("media_id", out var mediaId))
        {
            var id = mediaId.GetString() ?? "";
            LogApi("创建草稿成功", $"media_id={id}");
            return id;
        }

        var errmsg = root.TryGetProperty("errmsg", out var em) ? em.GetString() : "未知错误";
        LogApi("创建草稿失败", $"HTTP {response.StatusCode}\n请求: {JsonSerializer.Serialize(body)}\n响应: {json}");
        throw new InvalidOperationException($"创建草稿失败: {errmsg}");
    }

    private async Task<string> UploadImageAsync(string token, string imageFilePath)
    {
        var baseUrl = _settings.GetActiveWeChatConfig()!.ApiBaseUrl;
        var url = $"{baseUrl}/cgi-bin/material/add_material?access_token={token}&type=image";

        using var formData = new MultipartFormDataContent();
        var fileBytes = await File.ReadAllBytesAsync(imageFilePath);
        formData.Add(new ByteArrayContent(fileBytes), "media", Path.GetFileName(imageFilePath));

        var response = await _httpClient.PostAsync(url, formData);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("media_id", out var mediaId))
            return mediaId.GetString() ?? "";

        throw new InvalidOperationException($"上传图片失败: {json}");
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
