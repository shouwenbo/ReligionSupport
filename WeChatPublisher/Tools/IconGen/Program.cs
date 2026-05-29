using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Text.Json;

var secrets = JsonDocument.Parse(File.ReadAllText(
    @"f:\code\ReligionSupport\WeChatPublisher\Resources\secrets.json"));
var key = secrets.RootElement.GetProperty("HunyuanImageApiKey").GetString()!;

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}");
http.Timeout = TimeSpan.FromMinutes(3);

Console.WriteLine("Testing image generation via chat API...");

// Try image generation through chat completions with different models
var attempts = new[]
{
    ("hunyuan-image-3.0-instruct", "Generate a simple golden paper plane icon on dark blue background"),
    ("hunyuan-turbos-latest", "Generate an image: a golden paper plane icon on dark blue background"),
    ("hunyuan-vision", "Generate an image: a golden paper plane icon on dark blue background"),
};

foreach (var (model, prompt) in attempts)
{
    var body = new
    {
        model,
        messages = new[] { new { role = "user", content = prompt } },
        max_tokens = 500
    };
    var resp = await http.PostAsync("https://api.hunyuan.cloud.tencent.com/v1/chat/completions",
        new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
    var json = await resp.Content.ReadAsStringAsync();
    Console.WriteLine($"\nModel: {model} => {resp.StatusCode} ({json.Length} bytes)");
    if (json.Length > 0) Console.WriteLine($"  {json[..Math.Min(250, json.Length)]}");
}

// Also try different image endpoints
Console.WriteLine("\n\nTrying alternative image endpoints...");
var imgEndpoints = new[]
{
    "https://api.hunyuan.cloud.tencent.com/v1/images/generations",
    "https://api.hunyuan.cloud.tencent.com/openapi/v1/images/ar/generations",
    "https://hunyuan.tencentcloudapi.com/",
};

foreach (var url in imgEndpoints)
{
    var body = new { prompt = "icon", n = 1, size = "256x256" };
    try
    {
        var resp = await http.PostAsync(url,
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
        Console.WriteLine($"  {url} => {resp.StatusCode}");
    }
    catch (Exception ex) { Console.WriteLine($"  {url} => {ex.Message}"); }
}

Console.WriteLine("\nFalling back to code-generated icon...");
DrawIcon();

static void DrawIcon()
{
    var path = @"f:\code\ReligionSupport\WeChatPublisher\Assets\app.ico";
    using var bmp = new Bitmap(256, 256);
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

    // Gradient background
    var bg = new Rectangle(0, 0, 256, 256);
    using var bgBrush = new System.Drawing.Drawing2D.LinearGradientBrush(bg,
        Color.FromArgb(20, 20, 50), Color.FromArgb(40, 20, 60),
        System.Drawing.Drawing2D.LinearGradientMode.Vertical);
    g.FillRectangle(bgBrush, bg);

    // Glowing circle
    using var glowBrush = new SolidBrush(Color.FromArgb(30, 255, 215, 0));
    g.FillEllipse(glowBrush, 68, 45, 120, 120);
    using var glowBrush2 = new SolidBrush(Color.FromArgb(15, 255, 215, 0));
    g.FillEllipse(glowBrush2, 48, 25, 160, 160);

    // Paper plane (sending icon)
    using var gold = new SolidBrush(Color.FromArgb(255, 215, 0));
    var planePts = new[] {
        new Point(128, 35),
        new Point(185, 125),
        new Point(128, 108),
        new Point(71, 125)
    };
    g.FillPolygon(gold, planePts);

    // Wings
    using var pen = new Pen(Color.FromArgb(200, 255, 215, 0), 2.5f);
    g.DrawLine(pen, 128, 62, 172, 107);
    g.DrawLine(pen, 128, 62, 84, 107);

    // Motion lines
    var sparkles = new[] { (60, 95), (196, 95), (50, 120), (206, 120), (185, 75), (71, 75) };
    foreach (var (x, y) in sparkles)
    {
        using var b = new SolidBrush(Color.FromArgb(150, 255, 255, 255));
        g.FillEllipse(b, x, y, 3, 3);
    }

    // Cross
    using var crossPen = new Pen(Color.White, 1.2f);
    g.DrawLine(crossPen, 125, 72, 131, 72);
    g.DrawLine(crossPen, 128, 69, 128, 75);

    // Text
    using var font = new Font("Segoe UI", 7, FontStyle.Regular);
    using var textBrush = new SolidBrush(Color.FromArgb(80, 255, 255, 255));
    g.DrawString("WECHAT PUBLISHER", font, textBrush, new PointF(72, 230));

    // Rounded border
    using var borderPen = new Pen(Color.FromArgb(50, 255, 255, 255), 1.5f);
    g.DrawRoundedRectangle(borderPen, new Rectangle(3, 3, 250, 250), 40);

    // Save ICO
    SaveIco(bmp, path);
    Console.WriteLine($"ICO: {path}");
}

static void SaveIco(Bitmap bmp, string path)
{
    var sizes = new[] { 256, 128, 64, 48, 32, 16 };
    using var fs = new FileStream(path, FileMode.Create);
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
}

public static class Extensions
{
    public static void DrawRoundedRectangle(this Graphics g, Pen pen, Rectangle rect, int radius)
    {
        int d = radius * 2;
        g.DrawArc(pen, rect.X, rect.Y, d, d, 180, 90);
        g.DrawArc(pen, rect.Right - d, rect.Y, d, d, 270, 90);
        g.DrawArc(pen, rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        g.DrawArc(pen, rect.X, rect.Bottom - d, d, d, 90, 90);
        g.DrawLine(pen, rect.X + radius, rect.Y, rect.Right - radius, rect.Y);
        g.DrawLine(pen, rect.Right, rect.Y + radius, rect.Right, rect.Bottom - radius);
        g.DrawLine(pen, rect.Right - radius, rect.Bottom, rect.X + radius, rect.Bottom);
        g.DrawLine(pen, rect.X, rect.Bottom - radius, rect.X, rect.Y + radius);
    }
}
