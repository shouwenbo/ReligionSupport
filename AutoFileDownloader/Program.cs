namespace AutoFileDownloader
{
    internal class Program
    {
        static async Task Main(string[] args)
        {

            //string folderPath = @"F:\传道 & 助手\文字话术 & 问候语\长话术系列"; // 替换为你的文件夹路径
            //string outputFilePath = @"F:\传道 & 助手\文字话术 & 问候语\长话术 话术.txt"; // 合并后的文件路径


            string folderPath = @"F:\传道 & 助手\文字话术 & 问候语\一句话系列"; // 替换为你的文件夹路径
            string outputFilePath = @"F:\传道 & 助手\文字话术 & 问候语\一句话 话术.txt"; // 合并后的文件路径

            try
            {
                string[] txtFiles = Directory.GetFiles(folderPath, "*.txt", SearchOption.TopDirectoryOnly);

                using (StreamWriter writer = new StreamWriter(outputFilePath))
                {
                    foreach (string file in txtFiles)
                    {
                        writer.WriteLine($"----------");
                        string content = File.ReadAllText(file);
                        writer.WriteLine(content);
                        writer.WriteLine(); // 多加一个空行
                    }
                }

                Console.WriteLine("合并完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine("发生错误: " + ex.Message);
            }

            return;

            // var baseName = @"F:\学院\SH新41-E高级\启11章-3（补）";
            var baseName = @"F:\福音房\上海 午间CD分享\420622-青年财主的故事";

            var urlString =
@"
https://cdn.great-supper.com/20250622/220251_%E9%9D%92%E5%B9%B4%E8%B4%A2%E4%B8%BB%E7%9A%84%E6%95%85%E4%BA%8B.mp3
https://cdn.great-supper.com/20250622/220251_%E6%88%91%E4%BB%AC%E4%BB%8E%E6%9C%AA%E4%BA%86%E8%A7%A3%E7%9A%84%E8%80%B6%E7%A8%A3-1%EF%BC%88%E6%94%B9%EF%BC%89%20(1)_01.png

";

            List<string> urls = [.. urlString.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)];

            var audioUrls = urls.Where(u => u.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)).ToList();
            var imageUrls = urls.Where(u => u.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                            u.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                                            u.EndsWith(".png", StringComparison.OrdinalIgnoreCase)).ToList();

            await DownloadFilesAsync(audioUrls, baseName, isAudio: true);
            await DownloadFilesAsync(imageUrls, baseName, isAudio: false);

            Console.WriteLine("全部下载完成！");
        }

        static async Task DownloadFilesAsync(List<string> urls, string baseName, bool isAudio)
        {
            if (urls.Count == 0) return;

            string[] chineseNumbers = { "①", "②", "③", "④", "⑤", "⑥", "⑦", "⑧", "⑨", "⑩" };

            for (int i = 0; i < urls.Count; i++)
            {
                string url = urls[i];
                string extension = Path.GetExtension(url).ToLower();
                string fileName;

                if (urls.Count == 1)
                {
                    fileName = $"{baseName}{extension}";
                }
                else
                {
                    if (isAudio)
                        fileName = $"{baseName}（{i + 1}）{extension}";
                    else
                        fileName = $"{baseName}{chineseNumbers[i]}{extension}";
                }

                try
                {
                    Console.WriteLine($"正在下载：{url}");
                    using HttpClient client = new();
                    byte[] data = await client.GetByteArrayAsync(url);
                    await File.WriteAllBytesAsync(fileName, data);
                    Console.WriteLine($"已保存为：{fileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"下载失败：{url}，错误：{ex.Message}");
                }
            }
        }
    }
}
