using System;
using System.Diagnostics;
using System.Text;

namespace AutoMediaConvert
{
    internal class Program
    {
        static void MainBak()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            string ffmpegPath = @"D:\Program Files\ffmpeg-master-latest-win64-gpl\bin\ffmpeg.exe";
            string inputDir = @"F:\粮食\金鹏启示录";
            string outputDir = Path.Combine(inputDir, "MP3");

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var mp4Files = Directory.GetFiles(inputDir, "*.mp4", SearchOption.TopDirectoryOnly);

            Console.WriteLine($"共找到 {mp4Files.Length} 个 MP4 文件，开始转换...\n");

            foreach (var inputFile in mp4Files)
            {
                string fileName = Path.GetFileNameWithoutExtension(inputFile);
                string outputFile = Path.Combine(outputDir, fileName + ".mp3");

                Console.WriteLine($"\n🎬 正在转换：{fileName}.mp4");

                RunFfmpegWithProgress(ffmpegPath, inputFile, outputFile);
            }

            Console.WriteLine("\n✅ 所有文件转换完成！");
        }

        static void Main()
        {
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            string ffmpegPath = @"D:\Program Files\ffmpeg-master-latest-win64-gpl\bin\ffmpeg.exe";

            string[] choices = {
                "转换单个 MP4 文件",
                "批量转换文件夹下所有 MP4 文件"
            };

            string selected = ConsoleMenuSelector.Choose(choices, "请选择操作：");

            if (selected == choices[0])
            {
                Console.Write("请输入 MP4 文件路径：");
                string? inputFile = Console.ReadLine().Trim('"').Trim('"');
                if (!File.Exists(inputFile))
                {
                    Console.WriteLine("❌ 文件不存在。");
                    Console.ReadKey();
                    return;
                }

                string outputFile = Path.ChangeExtension(inputFile, ".mp3");
                Console.WriteLine($"\n🎬 正在转换：{Path.GetFileName(inputFile)}");
                RunFfmpegWithProgress(ffmpegPath, inputFile, outputFile);
            }
            else if (selected == choices[1])
            {
                Console.Write("请输入文件夹路径：");
                string inputDir = Console.ReadLine();
                if (!Directory.Exists(inputDir))
                {
                    Console.WriteLine("❌ 文件夹不存在。");
                    Console.ReadKey();
                    return;
                }

                string outputDir = Path.Combine(inputDir, "MP3");
                Directory.CreateDirectory(outputDir);

                var mp4Files = Directory.GetFiles(inputDir, "*.mp4", SearchOption.TopDirectoryOnly);
                Console.WriteLine($"\n共找到 {mp4Files.Length} 个 MP4 文件，开始转换...\n");

                foreach (var inputFile in mp4Files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(inputFile);
                    string outputFile = Path.Combine(outputDir, fileName + ".mp3");

                    Console.WriteLine($"\n🎬 正在转换：{fileName}.mp4");
                    RunFfmpegWithProgress(ffmpegPath, inputFile, outputFile);
                }

                Console.WriteLine("\n✅ 所有文件转换完成！");
            }

            Console.ReadKey();
        }

        static void RunFfmpegWithProgress(string ffmpegPath, string inputFile, string outputFile)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-y -i \"{inputFile}\" -q:a 0 -map a \"{outputFile}\"",
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };

            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data) && e.Data.Contains("time="))
                {
                    Console.Write("\r" + e.Data.PadRight(Console.WindowWidth - 1));
                }
            };

            process.Start();
            process.BeginErrorReadLine();
            process.WaitForExit();

            Console.WriteLine(); // 换行
            Console.WriteLine($"✅ 完成：{Path.GetFileName(inputFile)} → {Path.GetFileName(outputFile)}");
        }
    }
}
