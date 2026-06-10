using System.Text;
using System.Text.Json;

namespace WeChatPublisher.Services;

/// <summary>
/// HTTP MCP 客户端 - 连接外部MCP服务（如天上粮仓话语广场）
/// </summary>
public class HttpMcpClient
{
    private readonly HttpClient _http;
    private readonly string _endpoint;
    private readonly string _authHeader;

    public HttpMcpClient(string endpoint, string apiKey)
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _endpoint = endpoint.TrimEnd('/');
        _authHeader = $"Bearer {apiKey}";
    }

    private async Task<JsonElement> CallAsync(string method, object? args = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                method = "tools/call",
                @params = new
                {
                    name = method,
                    arguments = args ?? new { }
                },
                id = 1
            }), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", _authHeader);

        var resp = await _http.SendAsync(request);
        var json = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"MCP {method} 失败: HTTP {resp.StatusCode} - {json[..Math.Min(200, json.Length)]}");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // MCP JSON-RPC response
        if (root.TryGetProperty("result", out var result))
        {
            if (result.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in content.EnumerateArray())
                {
                    if (item.TryGetProperty("text", out var text))
                    {
                        var raw = text.GetString() ?? "";
                        return JsonDocument.Parse(raw).RootElement;
                    }
                }
            }
            return result;
        }

        var err = root.TryGetProperty("error", out var e) ? e.GetProperty("message").GetString() : "未知";
        throw new InvalidOperationException($"MCP {method} 错误: {err}");
    }

    /// <summary>随机取 N 段优质段落</summary>
    public async Task<List<string>> GetRandomParagraphs(int count = 8)
    {
        try
        {
            var data = await CallAsync("random_paragraphs", new { count });
            var items = new List<string>();
            if (data.ValueKind == JsonValueKind.Array)
                foreach (var item in data.EnumerateArray())
                    items.Add(item.GetProperty("content") is JsonElement c ? c.GetString() ?? "" : item.ToString());
            return items;
        }
        catch (Exception ex) { Logger.Warn($"MCP random_paragraphs 失败: {ex.Message}"); return []; }
    }

    /// <summary>随机取一段（快速版）</summary>
    public async Task<string?> GetDailyBread()
    {
        try
        {
            var data = await CallAsync("daily_bread");
            return data.TryGetProperty("content", out var c) ? c.GetString() : data.ToString();
        }
        catch (Exception ex) { Logger.Warn($"MCP daily_bread 失败: {ex.Message}"); return null; }
    }

    /// <summary>列出可用目录</summary>
    public async Task<string> ListCategories()
    {
        try
        {
            var data = await CallAsync("list_categories");
            return data.ToString();
        }
        catch (Exception ex) { Logger.Warn($"MCP list_categories 失败: {ex.Message}"); return ""; }
    }

    /// <summary>搜索文章</summary>
    public async Task<string> Search(string keyword)
    {
        try
        {
            var data = await CallAsync("search", new { keyword });
            return data.ToString();
        }
        catch (Exception ex) { Logger.Warn($"MCP search 失败: {ex.Message}"); return ""; }
    }
}
