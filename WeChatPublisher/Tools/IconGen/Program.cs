using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

var pngPath = @"f:\code\ReligionSupport\WeChatPublisher\Assets\app_icon.png";
if (!File.Exists(pngPath)) { Console.WriteLine("Source PNG not found"); return; }

Console.WriteLine("Processing icon: crop, resize, compress...");
using var src = new Bitmap(pngPath);

int size = Math.Min(src.Width, src.Height);
int x = (src.Width - size) / 2;
int y = (src.Height - size) / 2;
using var cropped = src.Clone(new Rectangle(x, y, size, size), src.PixelFormat);

using var sharpened = new Bitmap(size, size);
using (var g = Graphics.FromImage(sharpened))
{
    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    g.DrawImage(cropped, 0, 0, size, size);
}

var iconSizes = new[] { 256, 128, 64, 48, 32, 16 };
var icoPath = @"f:\code\ReligionSupport\WeChatPublisher\Assets\app.ico";
using (var fs = new FileStream(icoPath, FileMode.Create))
using (var bw = new BinaryWriter(fs))
{
    bw.Write((short)0); bw.Write((short)1); bw.Write((short)iconSizes.Length);
    var chunks = new List<byte[]>(); int offset = 6 + iconSizes.Length * 16;
    foreach (var s in iconSizes)
    {
        using var r = new Bitmap(sharpened, new Size(s, s));
        using var m = new MemoryStream(); r.Save(m, ImageFormat.Png);
        var d = m.ToArray(); chunks.Add(d);
        int sz = s == 256 ? 0 : s;
        bw.Write((byte)sz); bw.Write((byte)sz); bw.Write((byte)0); bw.Write((byte)0);
        bw.Write((short)1); bw.Write((short)32); bw.Write(d.Length); bw.Write(offset);
        offset += d.Length;
    }
    foreach (var c in chunks) bw.Write(c);
}
Console.WriteLine($"ICO: {icoPath} ({new FileInfo(icoPath).Length:N0} bytes)");

var previewPath = @"f:\code\ReligionSupport\WeChatPublisher\Assets\app_icon.png";
using (var preview = new Bitmap(sharpened, new Size(256, 256)))
{
    var tmp = previewPath + ".tmp";
    preview.Save(tmp, ImageFormat.Png);
    File.Move(tmp, previewPath, true);
}
Console.WriteLine($"PNG: {previewPath} ({new FileInfo(previewPath).Length:N0} bytes)");

var pubDir = @"f:\code\ReligionSupport\WeChatPublisher\bin\Release\net8.0-windows\publish\Assets";
if (Directory.Exists(pubDir))
{
    File.Copy(icoPath, Path.Combine(pubDir, "app.ico"), true);
    Console.WriteLine("Copied to publish output");
}
Console.WriteLine("Done!");
