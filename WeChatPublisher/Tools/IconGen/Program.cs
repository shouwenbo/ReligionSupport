using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Text.Json;

var secrets = JsonDocument.Parse(File.ReadAllText(
    @"f:\code\ReligionSupport\WeChatPublisher\Resources\secrets.json"));
var key = secrets.RootElement.GetProperty("TokenHubApiKey").GetString()!;
Console.WriteLine($"TokenHub Key: {key[..8]}...");

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}");
http.Timeout = TimeSpan.FromMinutes(3);

var prompt = "A modern minimalist app icon for a content publishing platform called WeChatPublisher. Features a glowing golden cross combined with a dove silhouette transforming into an upward-flying paper plane, symbolizing faith and communication. Deep blue to purple gradient background with subtle sparkle effects. The design should be clean, professional, instantly recognizable at small sizes. No text. High contrast. Circular rounded-square composition. This should look like a premium Windows application icon.";

Console.WriteLine("Generating icon with hy-image-v3.0...");
var body = new
{
    model = "hy-image-v3.0",
    prompt,
    n = 1,
    size = "1024x1024"
};

var resp = await http.PostAsync("https://tokenhub.tencentmaas.com/v1/images/generations",
    new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
var json = await resp.Content.ReadAsStringAsync();
Console.WriteLine($"HTTP {resp.StatusCode}");
Console.WriteLine($"Response: {json[..Math.Min(400, json.Length)]}");

using var doc = JsonDocument.Parse(json);
var root = doc.RootElement;

if (root.TryGetProperty("error", out var err))
{
    Console.WriteLine($"Error: {err}");
    return;
}

string? imageUrl = null;
if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
{
    var item = data[0];
    if (item.TryGetProperty("url", out var url)) imageUrl = url.GetString();
    if (item.TryGetProperty("b64_json", out var b64))
    {
        var bytes = Convert.FromBase64String(b64.GetString()!);
        SaveIcon(bytes);
        return;
    }
}

if (imageUrl != null)
{
    Console.WriteLine($"Downloading image: {imageUrl[..Math.Min(80, imageUrl.Length)]}");
    var imageBytes = await http.GetByteArrayAsync(imageUrl);
    SaveIcon(imageBytes);
}
else
{
    Console.WriteLine("No image URL found in response");
    Console.WriteLine("Full response: " + json);
}

static void SaveIcon(byte[] imageBytes)
{
    var pngPath = @"f:\code\ReligionSupport\WeChatPublisher\Assets\app_icon.png";
    File.WriteAllBytes(pngPath, imageBytes);
    Console.WriteLine($"PNG saved: {pngPath} ({imageBytes.Length} bytes)");

    using var ms = new MemoryStream(imageBytes);
    using var bmp = new Bitmap(ms);

    var sizes = new[] { 256, 128, 64, 48, 32, 16 };
    var icoPath = @"f:\code\ReligionSupport\WeChatPublisher\Assets\app.ico";
    using var fs = new FileStream(icoPath, FileMode.Create);
    using var bw = new BinaryWriter(fs);
    bw.Write((short)0); bw.Write((short)1); bw.Write((short)sizes.Length);
    var chunks = new List<byte[]>(); int offset = 6 + sizes.Length * 16;
    foreach (var s in sizes)
    {
        using var r = new Bitmap(bmp, new Size(s, s));
        using var m = new MemoryStream(); r.Save(m, ImageFormat.Png);
        var d = m.ToArray(); chunks.Add(d);
        int sz = s == 256 ? 0 : s;
        bw.Write((byte)sz); bw.Write((byte)sz); bw.Write((byte)0); bw.Write((byte)0);
        bw.Write((short)1); bw.Write((short)32); bw.Write(d.Length); bw.Write(offset);
        offset += d.Length;
    }
    foreach (var c in chunks) bw.Write(c);
    Console.WriteLine($"ICO saved: {icoPath} ({new FileInfo(icoPath).Length} bytes)");

    // Copy to publish
    var pubDir = @"f:\code\ReligionSupport\WeChatPublisher\bin\Release\net8.0-windows\publish\Assets";
    if (Directory.Exists(pubDir))
    {
        File.Copy(icoPath, Path.Combine(pubDir, "app.ico"), true);
        File.Copy(pngPath, Path.Combine(pubDir, "app_icon.png"), true);
        Console.WriteLine("Copied to publish output");
    }
    Console.WriteLine("Done!");
}
