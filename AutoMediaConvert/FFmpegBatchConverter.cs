using System.Diagnostics;

namespace AutoMediaConvert
{
    public class FFmpegBatchConverter
    {
        public static void ConvertAllMp4ToMp3(string ffmpegPath, string inputDirectory, string outputDirectory)
        {
            var mp4Files = Directory.GetFiles(inputDirectory, "*.mp4", SearchOption.TopDirectoryOnly);

            foreach (var inputFile in mp4Files)
            {
                var fileName = Path.GetFileNameWithoutExtension(inputFile);
                var outputFile = Path.Combine(outputDirectory, fileName + ".mp3");

                Console.WriteLine($"转换：{fileName}.mp4 -> {fileName}.mp3");

                ConvertSingle(ffmpegPath, inputFile, outputFile);
            }

            Console.WriteLine("全部转换完成！");
        }

        private static void ConvertSingle(string ffmpegPath, string inputFile, string outputFile)
        {
            var arguments = $"-i \"{inputFile}\" -q:a 0 -map a \"{outputFile}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            process.StandardOutput.ReadToEnd();  // 可省略
            process.StandardError.ReadToEnd();   // 可省略
            process.WaitForExit();
        }
    }
}
