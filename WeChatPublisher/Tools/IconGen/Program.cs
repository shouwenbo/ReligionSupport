using System.Text;
using System.Text.Json;

var secretsPath = @"f:\code\ReligionSupport\WeChatPublisher\Resources\secrets.json";
var secrets = JsonDocument.Parse(File.ReadAllText(secretsPath));
var key = secrets.RootElement.GetProperty("TokenHubApiKey").GetString()!;
Console.WriteLine($"TokenHub Key: {key[..8]}...");

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}");
http.Timeout = TimeSpan.FromMinutes(3);

var prompt = "Generate a modern minimalist app icon: glowing golden cross combined with a dove transforming into an upward paper plane. Deep blue to purple gradient background (hex #1a1a2e to #16213e). Clean flat design, no text, high contrast, circular rounded-square composition. The icon should be beautiful and recognizable at 256x256, 64x64, and 32x32 sizes. Save as PNG.";

Console.WriteLine("Calling TokenHub image generation...");
var body = new
{
    model = "ep-km3k66ay",
    instructions = "You are an image generator. Generate the image exactly as described by the user.",
    input = prompt,
    stream = false
};

var resp = await http.PostAsync("https://tokenhub.tencentmaas.com/v1/responses",
    new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
var json = await resp.Content.ReadAsStringAsync();
Console.WriteLine($"HTTP {resp.StatusCode}");
Console.WriteLine($"Response ({json.Length} chars): {json[..Math.Min(500, json.Length)]}");

using var doc = JsonDocument.Parse(json);
var root = doc.RootElement;

// Check for error
if (root.TryGetProperty("error", out var error))
{
    Console.WriteLine($"API Error: {error}");
    return;
}

// Check response fields
Console.WriteLine("\nResponse fields:");
foreach (var prop in root.EnumerateObject())
    Console.WriteLine($"  {prop.Name}: {prop.Value.ValueKind}");

// Try to extract image
string? imageUrl = null;
string? base64 = null;

if (root.TryGetProperty("output", out var output))
{
    var text = output.GetString() ?? "";
    Console.WriteLine($"\nOutput preview: {text[..Math.Min(300, text.Length)]}");

    // Look for URLs
    foreach (var word in text.Split(' ', '\n', '\r'))
    {
        var w = word.Trim();
        if (w.StartsWith("http") && (w.Contains(".png") || w.Contains(".jpg") || w.Contains("image")))
            imageUrl = w;
        if (w.StartsWith("data:image"))
            base64 = w.Split(",", 2).Last();
    }
}

// Check for direct url
if (root.TryGetProperty("url", out var urlProp))
    imageUrl = urlProp.GetString();

// Check for data
if (root.TryGetProperty("data", out var data))
{
    if (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
    {
        var item = data[0];
        if (item.TryGetProperty("url", out var u)) imageUrl = u.GetString();
        if (item.TryGetProperty("b64_json", out var b)) base64 = b.GetString();
    }
}

if (imageUrl != null)
{
    Console.WriteLine($"Downloading: {imageUrl[..Math.Min(80, imageUrl.Length)]}");
    var bytes = await http.GetByteArrayAsync(imageUrl);
    File.WriteAllBytes(@"f:\code\ReligionSupport\WeChatPublisher\Assets\app_icon.png", bytes);
    Console.WriteLine($"PNG saved ({bytes.Length} bytes)");
    Console.WriteLine("Now convert to ICO manually or run icon tool");
}
else if (base64 != null)
{
    var bytes = Convert.FromBase64String(base64);
    File.WriteAllBytes(@"f:\code\ReligionSupport\WeChatPublisher\Assets\app_icon.png", bytes);
    Console.WriteLine($"PNG saved from base64 ({bytes.Length} bytes)");
}
else
{
    Console.WriteLine("No image found in response. Full response:");
    Console.WriteLine(json);
}
