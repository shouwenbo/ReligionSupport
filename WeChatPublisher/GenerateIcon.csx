#!/usr/bin/env dotnet-script
#r "nuget: System.Drawing.Common, 8.0.0"

using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Text.Json;

var secrets = JsonDocument.Parse(File.ReadAllText(
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "secrets.json")));
var key = secrets.RootElement.GetProperty("HunyuanImageApiKey").GetString()!;

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}");

var prompt = "A minimalist, modern app icon for a content publishing platform. Features a stylized paper plane made of glowing blue and white gradient, launching upward with subtle sparkles. Dark background with rounded corners. The design should be clean, professional, and instantly recognizable at small sizes. No text. High contrast, flat design style suitable for Windows application icon.";

var body = new { model = "hunyuan-image-3.0-instruct", prompt, n = 1, size = "1024x1024" };
var resp = await http.PostAsync("https://api.hunyuan.cloud.tencent.com/v1/images/generations",
    new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
var json = await resp.Content.ReadAsStringAsync();
Console.WriteLine(json);

using var jdoc = JsonDocument.Parse(json);
var url = jdoc.RootElement.GetProperty("data")[0].GetProperty("url").GetString()!;
var bytes = await http.GetByteArrayAsync(url);
File.WriteAllBytes(@"f:\code\ReligionSupport\WeChatPublisher\Assets\app_icon.png", bytes);
Console.WriteLine("Icon saved to Assets/app_icon.png");
