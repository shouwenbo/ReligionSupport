// ====== 替换整个文件: F:\code\ReligionSupport\AutoVideoClipper.WPF\MainWindow.xaml.cs ======
using AutoClipper;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace AutoVideoClipper.WPF
{
    public partial class MainWindow : Window
    {
        private string? audioPath;
        private string? videoFolderPath;
        private string? musicFolderPath;

        private readonly string ffmpegExe = @"Assets\ffmpeg.exe";
        private readonly string ffprobeExe = @"Assets\ffprobe.exe";
        private readonly string workDir;
        // 新增：目标根目录（固定）
        private readonly string targetRoot = @"F:\视频号\短视频";

        public MainWindow()
        {
            InitializeComponent();

            if (!Directory.Exists(targetRoot))
            {
                // 隐藏经文、经文内容、简介三行
                VerseRow.Visibility = Visibility.Collapsed;
                VerseContentRow.Visibility = Visibility.Collapsed;
                SummaryRow.Visibility = Visibility.Collapsed;

                Log("⚠ 检测到目标根目录不存在，将隐藏经文、简介等输入区，并跳过相关文本输出。");
            }

            workDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "work");
            ClearWorkFolder();
            Directory.CreateDirectory(workDir);

            FFmpegRunner.InitializeLog();

            LoadLastPaths();
            OutputSourceFilesToTxt();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.5)));
            this.BeginAnimation(Window.OpacityProperty, fadeIn);
        }

        /// <summary>
        /// 清空工作文件夹
        /// </summary>
        private void ClearWorkFolder()
        {
            if (Directory.Exists(workDir))
            {
                DirectoryInfo di = new DirectoryInfo(workDir);
                foreach (var file in di.GetFiles()) file.Delete();
                foreach (var dir in di.GetDirectories()) dir.Delete(true);
            }
        }

        #region 路径保存与加载
        private void LoadLastPaths()
        {
            videoFolderPath = AutoClipper.Properties.Settings.Default.LastVideoFolderPath;
            musicFolderPath = AutoClipper.Properties.Settings.Default.LastMusicFolderPath;

            TbFolder.Text = videoFolderPath;
            TbMusicFolder.Text = musicFolderPath;

            // ✅ 加载标题和正文
            TbTitle.Text = AutoClipper.Properties.Settings.Default.LastTitle;
            TbContent.Text = AutoClipper.Properties.Settings.Default.LastContent;

            // ✅ 加载经文、经文内容、简介
            TbVerse.Text = AutoClipper.Properties.Settings.Default.LastVerse;
            TbVerseContent.Text = AutoClipper.Properties.Settings.Default.LastVerseContent;
            TbSummary.Text = AutoClipper.Properties.Settings.Default.LastSummary;
        }

        private void SavePaths()
        {
            // AutoClipper.Properties.Settings.Default.LastVideoFolderPath = videoFolderPath;
            // AutoClipper.Properties.Settings.Default.LastMusicFolderPath = musicFolderPath;
            // 
            // // ✅ 保存标题和正文
            // AutoClipper.Properties.Settings.Default.LastTitle = TbTitle.Text.Trim();
            // AutoClipper.Properties.Settings.Default.LastContent = TbContent.Text.Trim();
            // 
            // // ✅ 保存经文、经文内容、简介
            // AutoClipper.Properties.Settings.Default.LastVerse = TbVerse.Text.Trim();
            // AutoClipper.Properties.Settings.Default.LastVerseContent = TbVerseContent.Text.Trim();
            // AutoClipper.Properties.Settings.Default.LastSummary = TbSummary.Text.Trim();
            // 
            // AutoClipper.Properties.Settings.Default.Save();
        }
        #endregion

        #region 文件选择事件

        private void BtnPickFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                videoFolderPath = dlg.SelectedPath;
                TbFolder.Text = videoFolderPath;
                SavePaths();
            }
        }

        private void BtnPickMusicFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                musicFolderPath = dlg.SelectedPath;
                TbMusicFolder.Text = musicFolderPath;
                SavePaths();
            }
        }
        #endregion

        private void BtnOpenWork_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(workDir)) Directory.CreateDirectory(workDir);
            Process.Start(new ProcessStartInfo { FileName = workDir, UseShellExecute = true });
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            BtnStart.IsEnabled = false;

            try
            {
                string title = TbTitle.Text.Trim();
                string content = TbContent.Text.Trim();

                // ✅ 标题校验
                var titleParts = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (titleParts.Length != 2)
                {
                    Log("❌ 标题格式错误：必须由两个词语组成，中间用空格隔开！");
                    BtnStart.IsEnabled = true;
                    return;
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    Log("❌ 正文内容不能为空！");
                    BtnStart.IsEnabled = true;
                    return;
                }

                // ✅ 检查视频目录
                if (string.IsNullOrWhiteSpace(videoFolderPath))
                {
                    Log("❌ 未选择视频文件夹！");
                    BtnStart.IsEnabled = true;
                    return;
                }

                // ✅ 生成音频
                string tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp");
                Directory.CreateDirectory(tempDir);
                string audioFileName = $"{titleParts[0]}_{titleParts[1]}.mp3";
                string audioOutPath = Path.Combine(tempDir, audioFileName);

                Log("🎙 开始生成音频...");
                var ttsService = new TtsService(Log);
                audioPath = await ttsService.GenerateAudioAsync(content, audioOutPath);

                if (string.IsNullOrWhiteSpace(audioPath) || !File.Exists(audioPath))
                {
                    Log("❌ 音频生成失败，请检查网络或TTS服务。");
                    BtnStart.IsEnabled = true;
                    return;
                }

                Log("✅ 音频生成成功：" + audioPath);
                await ProcessVideoAsync(titleParts[0], titleParts[1]);
                Log("✅ 视频生成完成！详细日志请查看 ffmpeg.log");
            }
            catch (Exception ex)
            {
                Log($"❌ 发生错误: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                SavePaths();
                BtnStart.IsEnabled = true;
            }
        }

        #region 核心处理逻辑
        private async Task ProcessVideoAsync(string titleWord1, string titleWord2)
        {
            double extraSec = double.TryParse(TbExtraSeconds.Text, out var es) ? es : 3;
            Log("获取音频时长...");
            double audioDuration = await FFmpegRunner.GetDurationAsync(ffprobeExe, audioPath);
            double targetDuration = audioDuration + extraSec;
            Log($"音频时长: {audioDuration:F2}s, 目标视频时长: {targetDuration:F2}s");

            string videoFile = PickRandomVideo();
            double videoDuration = await FFmpegRunner.GetDurationAsync(ffprobeExe, videoFile);
            double scale = targetDuration / videoDuration;

            string tempVideo = Path.Combine(workDir, "temp_video.mp4");
            await GenerateTempVideoAsync(videoFile, tempVideo, scale, targetDuration, audioPath, titleWord1, titleWord2);

            string? bgAudio = await PickAndTrimBackgroundAudio(targetDuration);

            string finalOut = Path.Combine(workDir, "final_video.mp4");

            if (bgAudio != null)
            {
                string tempMixed = Path.Combine(workDir, "video_with_audio.mp4");
                await MixBackgroundAudio(tempVideo, bgAudio, tempMixed);
                finalOut = await SetVideoCoverAsync(tempMixed, finalOut, titleWord1, titleWord2);
            }
            else
            {
                finalOut = await SetVideoCoverAsync(tempVideo, finalOut, titleWord1, titleWord2);
            }

            Log($"最终视频生成成功: {finalOut}");

            // ===== ✅ 新增压缩逻辑（提前到复制之前） =====
            await CompressFinalVideoAsync(ffmpegExe, finalOut, workDir, Log);

            // ===== 新增：生成目标文件夹、复制视频、生成文本并打开文件夹 =====
            try
            {
                await CreateFolderAndCopyFilesAsync(finalOut, titleWord1, titleWord2);
            }
            catch (Exception ex)
            {
                Log("⚠ 自动复制/生成目标文件夹时出错: " + ex.Message);
            }
        }

        private static async Task CompressFinalVideoAsync(string ffmpegExe, string finalOut, string workDir, Action<string> Log)
        {
            try
            {
                string compressedOut = Path.Combine(workDir, "final_video_compressed.mp4");
                Log("🎬 开始压缩视频（保留封面）...");

                // 第一步：压缩视频（CRF 28 可自行调整画质）
                string compressCmd = $"-y -i \"{finalOut}\" -map 0:v -map 0:a? " +
                                     "-c:v libx264 -preset slow -crf 28 " +
                                     "-c:a aac -b:a 128k -movflags +faststart " +
                                     $"\"{compressedOut}\"";
                await FFmpegRunner.RunAsync(ffmpegExe, compressCmd, Log);

                // 第二步：复制封面（如果原视频有封面流）
                string tmpFile = $"{compressedOut}.tmp.mp4";
                string copyCoverCmd = $"-y -i \"{finalOut}\" -i \"{compressedOut}\" " +
                                      "-map 1:v -map 1:a? -map 0:v:1? " +
                                      "-c copy -disposition:v:1 attached_pic " +
                                      $"\"{tmpFile}\"";
                await FFmpegRunner.RunAsync(ffmpegExe, copyCoverCmd, Log);

                // 第三步：替换文件
                if (File.Exists(tmpFile))
                {
                    File.Copy(tmpFile, compressedOut, true);
                    File.Delete(tmpFile);
                }

                File.Copy(compressedOut, finalOut, true);

                // 输出压缩结果
                var origSize = new FileInfo(finalOut).Length / 1024 / 1024.0;
                var newSize = new FileInfo(compressedOut).Length / 1024 / 1024.0;
                Log($"✅ 压缩完成并保留封面：原始 {origSize:F1} MB → 压缩后 {newSize:F1} MB");
                Log("📦 已用压缩版替换最终视频文件。");
            }
            catch (Exception ex)
            {
                Log("⚠ 视频压缩或封面复制失败：" + ex.Message);
            }
        }


        private string PickRandomVideo()
        {
            var videoFiles = Directory.GetFiles(videoFolderPath!, "*.*")
                .Where(f => f.EndsWith(".mp4") || f.EndsWith(".mov") || f.EndsWith(".mkv"))
                .ToArray();
            if (!videoFiles.Any()) throw new Exception("视频文件夹中没有视频文件！");
            string chosen = videoFiles[new Random().Next(videoFiles.Length)];
            Log("选中视频: " + chosen);
            return chosen;
        }

        private async Task GenerateTempVideoAsync(string inputVideo, string output, double scale, double duration, string? voiceAudio, string titleWord1, string titleWord2)
        {
            // 获取视频宽高信息
            var info = await FFmpegRunner.GetVideoInfoAsync(ffprobeExe, inputVideo);
            int inWidth = info.Width;
            int inHeight = info.Height;

            // 视频画面缩放、裁剪
            string scaleFilter = "scale=-1:1440";
            int cropWidth = Math.Min(1080, (int)Math.Round(inWidth * (1440.0 / inHeight)));
            string cropFilter = $"crop={cropWidth}:1440:(iw-{cropWidth})/2:0";

            // ======= 标题生成部分 =======
            string line1 = FFmpegRunner.EscapeFfmpegText(titleWord1);
            string line2 = FFmpegRunner.EscapeFfmpegText(titleWord2);

            // 字体路径
            string fontPath = @"C\:/Windows/Fonts/msyh.ttc";
            if (!File.Exists(@"C:\Windows\Fonts\msyh.ttc"))
            {
                if (File.Exists(@"C:\Windows\Fonts\simhei.ttf"))
                    fontPath = @"C\:/Windows/Fonts/simhei.ttf";
                else
                    Log("⚠ 未找到系统字体，文字可能无法正常显示中文！");
            }

            // ======= 字幕处理部分 =======
            string? srtFile = null;
            if (!string.IsNullOrWhiteSpace(voiceAudio))
            {
                try
                {
                    Log("尝试通过在线接口生成字幕...");
                    string? subtitleUrl = await new SubtitleGenerator(Log).TryGenerateSubtitleAsync(voiceAudio, "zh-CN");

                    if (!string.IsNullOrWhiteSpace(subtitleUrl))
                    {
                        if (subtitleUrl.StartsWith("/"))
                            subtitleUrl = "https://www.text-to-speech.cn" + subtitleUrl;
                        else if (!subtitleUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                            subtitleUrl = "https://www.text-to-speech.cn/" + subtitleUrl;

                        Log("✅ 在线字幕生成成功：" + subtitleUrl);

                        string tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp");
                        Directory.CreateDirectory(tempDir);
                        srtFile = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(voiceAudio) + ".srt");

                        using var http = new HttpClient();
                        try
                        {
                            var bytes = await http.GetByteArrayAsync(subtitleUrl);
                            await File.WriteAllBytesAsync(srtFile, bytes);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("❌ 在线字幕下载失败：" + ex.Message);
                        }

                        if (!File.Exists(srtFile))
                            throw new Exception("❌ 字幕文件下载后不存在！");

                        Log($"✅ 字幕文件已下载: {srtFile}");
                    }
                    else
                    {
                        throw new Exception("❌ 在线字幕生成接口返回空，生成失败！");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("❌ 在线字幕生成失败: " + ex.Message);
                }
            }

            // ======= drawtext 标题滤镜 =======
            int titleFontSize = 100;
            string drawText =
                $"drawtext=fontfile='{fontPath}':text='{line1}':fontcolor=#FFDE00:fontsize={titleFontSize}:borderw=4:bordercolor=#3C5C37:" +
                $"x=(w-text_w)/2:y=h*0.06," +
                $"drawtext=fontfile='{fontPath}':text='{line2}':fontcolor=#FFDE00:fontsize={titleFontSize}:borderw=4:bordercolor=#3C5C37:" +
                $"x=(w-text_w)/2:y=h*0.15";

            // ======= 核心滤镜组合 =======
            string filter =
                $"[0:v]setpts=PTS*{scale.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                $"{scaleFilter},{cropFilter},{drawText}";

            // ======= 若有字幕文件，叠加下方字幕 =======
            if (!string.IsNullOrWhiteSpace(srtFile) && File.Exists(srtFile))
            {
                string subtitleFontName = "Microsoft YaHei";
                string tempSrt = Path.Combine(Path.GetDirectoryName(srtFile)!, "temp_subtitle.srt");

                var lines = File.ReadAllLines(srtFile, Encoding.UTF8).ToList();
                for (int i = 0; i < lines.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(lines[i]) &&
                        !lines[i].Contains("-->") &&
                        !int.TryParse(lines[i], out _))
                    {
                        string text = lines[i].Trim();
                        text = Regex.Replace(text, @"^[，。！？：；、“”‘’""'\s]+", "");
                        text = Regex.Replace(text, @"[，。！？：；、“”‘’""'\s]+$", "");

                        int maxCharsPerLine = 8;
                        if (text.Length > maxCharsPerLine)
                        {
                            var newText = "";
                            for (int j = 0; j < text.Length; j += maxCharsPerLine)
                            {
                                if (j > 0) newText += "\\N";
                                newText += text.Substring(j, Math.Min(maxCharsPerLine, text.Length - j));
                            }
                            lines[i] = newText;
                        }
                        else
                        {
                            lines[i] = text;
                        }
                    }
                }

                File.WriteAllLines(tempSrt, lines, Encoding.UTF8);

                string escapedSrt = tempSrt.Replace("\\", "/").Replace(":", "\\:");
                int subtitleFontSize = 18;
                int marginH = (int)(cropWidth * 0.06);
                int marginV = 50;

                string forceStyle =
                    $"Alignment=2," +
                    $"MarginV={marginV}," +
                    $"MarginL={marginH}," +
                    $"MarginR={marginH}," +
                    $"Fontname={subtitleFontName}," +
                    $"Fontsize={subtitleFontSize}," +
                    $"Outline=2,Shadow=1,BorderStyle=1";

                filter += $",subtitles='{escapedSrt}':force_style='{forceStyle}'";
            }

            // ======= 生成中央经文字幕（drawtext方式） =======
            string verseText = TbVerseContent.Text;
            double verseDuration = 0;
            double.TryParse(TbExtraSeconds.Text, out verseDuration);

            if (!string.IsNullOrWhiteSpace(verseText) && verseDuration > 0)
            {
                // 换行处理，每行最多8个字
                int maxCharsPerLine = 8;
                var verseLines = new System.Collections.Generic.List<string>();
                for (int i = 0; i < verseText.Length; i += maxCharsPerLine)
                {
                    verseLines.Add(verseText.Substring(i, Math.Min(maxCharsPerLine, verseText.Length - i)));
                }
                string verseWithNewLine = string.Join("\\n", verseLines); // drawtext换行符

                double startTime = duration; // 从视频末尾开始显示

                int verseFontSize = 18;
                filter += $",drawtext=fontfile='{fontPath}':text='{verseWithNewLine}':" +
                          $"fontcolor=white:fontsize={verseFontSize}:" +
                          $"x=(w-text_w)/2:y=(h-text_h)/2:" +
                          $"enable='between(t,{startTime},{startTime + verseDuration})'";
            }

            filter += ",format=yuv420p[v]";

            // ======= FFmpeg 命令生成 =======
            string cmd;
            if (!string.IsNullOrWhiteSpace(voiceAudio))
            {
                cmd = $"-y -i \"{inputVideo}\" -i \"{voiceAudio}\" -filter_complex \"{filter}\" " +
                      "-map \"[v]\" -map 1:a -r 30 -c:v libx264 -preset veryfast -crf 20 -c:a aac \"" + output + "\"";
            }
            else
            {
                cmd = $"-y -i \"{inputVideo}\" -filter_complex \"{filter}\" " +
                      "-map \"[v]\" -map 0:a -r 30 -c:v libx264 -preset veryfast -crf 20 -c:a aac \"" + output + "\"";
            }

            await FFmpegRunner.RunAsync(ffmpegExe, cmd, Log);
            Log("临时视频生成完成: " + output);
        }


        /// <summary>
        /// 查找本地字幕文件（辅助方法）
        /// </summary>
        private string? FindLocalSrt(string voiceAudio)
        {
            string dir = Path.GetDirectoryName(voiceAudio)!;
            var srtFiles = Directory.GetFiles(dir, "*.srt");
            if (srtFiles.Length == 1)
            {
                Log("检测到字幕文件: " + srtFiles[0]);
                return srtFiles[0];
            }
            if (srtFiles.Length > 1)
                Log("⚠ 检测到多个 SRT 文件，未使用字幕。");
            else
                Log("未找到 SRT 字幕文件。");
            return null;
        }

        private async Task<string?> PickAndTrimBackgroundAudio(double targetDuration)
        {
            if (string.IsNullOrWhiteSpace(musicFolderPath)) return null;

            var mp3Files = Directory.GetFiles(musicFolderPath!, "*.mp3");
            if (!mp3Files.Any()) return null;

            string chosen = mp3Files[new Random().Next(mp3Files.Length)];
            double bgDuration = await FFmpegRunner.GetDurationAsync(ffprobeExe, chosen);

            if (bgDuration > targetDuration)
            {
                string tmpAudio = Path.Combine(workDir, "bg_cut.mp3");
                double start = (bgDuration - targetDuration) / 2;
                await FFmpegRunner.RunAsync(ffmpegExe, $"-y -i \"{chosen}\" -ss {start} -t {targetDuration} -c copy \"{tmpAudio}\"", Log);
                chosen = tmpAudio;
            }

            Log("选中背景音乐: " + chosen);
            return chosen;
        }

        private async Task MixBackgroundAudio(string videoFile, string bgAudio, string output)
        {
            // audioPath 是人声
            // 目标：让背景音乐响度自动匹配人声，始终比人声低约 6dB

            string cmd =
                $"-y -i \"{videoFile}\" -i \"{bgAudio}\" -filter_complex " +
                "\"[0:a]loudnorm=I=-16:TP=-1.5:LRA=11[a0norm];" +        // 归一化人声
                "[1:a]loudnorm=I=-22:TP=-2:LRA=11[a1norm];" +             // 归一化背景音乐
                "[a0norm]volume=1.0[a0];" +                               // 保持人声原响度
                "[a1norm]volume=0.8[a1];" +                               // 背景音乐稍弱
                "[a0][a1]amix=inputs=2:duration=longest:dropout_transition=2[a]\" " +
                "-map 0:v -map \"[a]\" -c:v copy -c:a aac -b:a 192k -movflags +faststart \"" + output + "\"";

            await FFmpegRunner.RunAsync(ffmpegExe, cmd, Log);
            Log("人声与背景音乐动态响度混合完成: " + output);
        }


        private async Task<string> SetVideoCoverAsync(string videoFile, string outputFile, string titleWord1, string titleWord2)
        {
            string coverImage = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "default_cover.jpg");
            if (!File.Exists(coverImage)) FFmpegRunner.GenerateDefaultCover(coverImage);

            // 1️⃣ 临时封面路径
            string coverTmp = Path.Combine(workDir, "cover_tmp.jpg");
            string coverWithText = Path.Combine(workDir, "cover_with_text.jpg");

            // 2️⃣ 按视频比例裁剪封面（1080x1440）
            string cropCmd = $"-y -i \"{coverImage}\" -vf \"scale=-1:1440,crop=1080:1440:(iw-1080)/2:0\" \"{coverTmp}\"";
            await FFmpegRunner.RunAsync(ffmpegExe, cropCmd, Log);

            // 3️⃣ 从音频名中提取标题
            string line1 = FFmpegRunner.EscapeFfmpegText(titleWord1);
            string line2 = FFmpegRunner.EscapeFfmpegText(titleWord2);

            // 4️⃣ 检查字体路径
            string fontPath = @"C\:/Windows/Fonts/msyh.ttc";
            if (!File.Exists(@"C:\Windows\Fonts\msyh.ttc"))
            {
                if (File.Exists(@"C:\Windows\Fonts\simhei.ttf"))
                    fontPath = @"C\:/Windows/Fonts/simhei.ttf";
                else
                    Log("⚠ 未找到系统字体，封面文字可能无法显示中文！");
            }

            // 5️⃣ 绘制标题（与视频一致）
            int titleFontSize = 100;
            string drawTextFilter =
                $"drawtext=fontfile='{fontPath}':text='{line1}':fontcolor=#FFFFFF:fontsize={titleFontSize}:borderw=4:bordercolor=#8487F3:" +
                $"x=(w-text_w)/2:y=h*0.06," +
                $"drawtext=fontfile='{fontPath}':text='{line2}':fontcolor=#FFFFFF:fontsize={titleFontSize}:borderw=4:bordercolor=#8487F3:" +
                $"x=(w-text_w)/2:y=h*0.15";

            string drawCmd = $"-y -i \"{coverTmp}\" -vf \"{drawTextFilter}\" \"{coverWithText}\"";
            await FFmpegRunner.RunAsync(ffmpegExe, drawCmd, Log);

            // 6️⃣ 封面写入视频（使用加了标题的图）
            string cmd = $"-y -i \"{videoFile}\" -i \"{coverWithText}\" -map 0 -map 1 " +
                         "-c:v copy -c:a aac -b:a 128k -disposition:v:1 attached_pic \"" + outputFile + "\"";

            await FFmpegRunner.RunAsync(ffmpegExe, cmd, Log);
            Log("封面图片已设置完成: " + outputFile);



            // // 7️⃣ 生成 0.1 秒的封面视频并插入到最前面
            // string coverVideo = Path.Combine(workDir, "cover_0.1s.mp4");
            // 
            // // 7.1️⃣ 用封面图生成 0.1 秒视频（帧率30，添加静音音轨，防止音频丢失）
            // string coverVideoCmd = $"-y -loop 1 -i \"{coverWithText}\" -f lavfi -i anullsrc=channel_layout=stereo:sample_rate=44100 " +
            //                        "-shortest -t 0.1 -r 30 -pix_fmt yuv420p -c:v libx264 -c:a aac -b:a 128k \"" + coverVideo + "\"";
            // await FFmpegRunner.RunAsync(ffmpegExe, coverVideoCmd, Log);
            // 
            // // 7.2️⃣ 使用 filter_complex 安全拼接
            // string finalOutput = Path.Combine(workDir, "final_with_cover.mp4");
            // string concatCmd = $"-y -i \"{coverVideo}\" -i \"{outputFile}\" " +
            //                    "-filter_complex \"[0:v:0][0:a:0][1:v:0][1:a:0]concat=n=2:v=1:a=1[outv][outa]\" " +
            //                    "-map \"[outv]\" -map \"[outa]\" -c:v libx264 -crf 20 -preset veryfast -c:a aac -b:a 128k \"" + finalOutput + "\"";
            // 
            // await FFmpegRunner.RunAsync(ffmpegExe, concatCmd, Log);
            // Log("✅ 已在视频前插入 0.1 秒的封面片头: " + finalOutput);

            // ===== 第7步：生成0.1秒封面视频并插入到最前面 =====
            string coverVideo = Path.Combine(workDir, "cover_0.1s.mp4");
            string finalOutput = Path.Combine(workDir, "final_with_cover.mp4");

            // 7.0️⃣ 获取原视频信息
            var videoInfo = await FFmpegRunner.GetVideoInfoAsync(ffprobeExe, outputFile); // 自己实现获取 width/height/fps/SAR
            int width = videoInfo.Width;
            int height = videoInfo.Height;
            double fps = videoInfo.Fps;
            string sar = $"{videoInfo.SarNum}:{videoInfo.SarDen}";

            // 7.1️⃣ 用封面图生成 0.1 秒视频（帧率和尺寸与原视频一致，添加静音音轨）
            string coverVideoCmd = $"-y -loop 1 -i \"{coverWithText}\" " +
                                   $"-f lavfi -i anullsrc=channel_layout=stereo:sample_rate=44100 " +
                                   $"-vf \"scale={width}:{height},setsar={sar}\" " +
                                   $"-shortest -t 0.1 -r {fps} -pix_fmt yuv420p -c:v libx264 -c:a aac -b:a 128k \"{coverVideo}\"";
            await FFmpegRunner.RunAsync(ffmpegExe, coverVideoCmd, Log);

            // 7.2️⃣ 使用 filter_complex 安全拼接
            string concatCmd = $"-y -i \"{coverVideo}\" -i \"{outputFile}\" " +
                               "-filter_complex \"[0:v:0][0:a:0][1:v:0][1:a:0]concat=n=2:v=1:a=1[outv][outa]\" " +
                               "-map \"[outv]\" -map \"[outa]\" -c:v libx264 -crf 20 -preset veryfast -c:a aac -b:a 128k \"" + finalOutput + "\"";
            await FFmpegRunner.RunAsync(ffmpegExe, concatCmd, Log);

            Log("✅ 已在视频前插入 0.1 秒的封面片头: " + finalOutput);

            return finalOutput;

        }

        private void TbVerse_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                var verse_content = new BibleService().QueryBible_SqlList版本(TbVerse.Text, 4);
                TbVerseContent.Text = verse_content;
            }
        }

        #endregion

        #region 日志
        private void Log(string msg)
        {
            Dispatcher.Invoke(() =>
            {
                TbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n");
                TbLog.ScrollToEnd();
            }, DispatcherPriority.Background);
        }
        #endregion

        #region 输出源码到txt
        private void OutputSourceFilesToTxt()
        {
            try
            {
                string[] files =
                {
                    @"F:\code\ReligionSupport\AutoVideoClipper.WPF\MainWindow.xaml",
                    @"F:\code\ReligionSupport\AutoVideoClipper.WPF\MainWindow.xaml.cs",
                    @"F:\code\ReligionSupport\AutoVideoClipper.WPF\FFmpegRunner.cs"
                };

                var sb = new StringBuilder();
                foreach (var file in files)
                {
                    if (File.Exists(file))
                    {
                        sb.AppendLine($"===== 文件: {file} =====");
                        sb.AppendLine(File.ReadAllText(file));
                        sb.AppendLine();
                    }
                    else
                        sb.AppendLine($"===== 文件未找到: {file} =====\n");
                }

                string outputPath = Path.Combine(workDir, "AllSourceOutput.txt");
                File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
                Log($"源码已输出到 {outputPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"输出源码失败: {ex.Message}");
            }
        }
        #endregion

        #region 新增：按规则生成目标文件夹并复制文件（实现用户需求）
        /// <summary>
        /// 主流程：在 targetRoot 下查找最近的 yymmdd 前缀文件夹，+2 天生成新文件夹，复制 final 视频并生成文本文件，最后打开文件夹。
        /// </summary>
        private async Task CreateFolderAndCopyFilesAsync(string finalVideoPath, string titleWord1, string titleWord2)
        {
            if (!File.Exists(finalVideoPath))
            {
                Log("❌ final 视频不存在，无法复制到目标文件夹。");
                return;
            }

            string baseRoot = targetRoot;
            bool fallbackToProjectDir = false;

            string newFolderPath;

            // 如果根目录不存在，退回到项目目录
            if (!Directory.Exists(baseRoot))
            {
                Log($"⚠ 目标根目录不存在：{baseRoot}，将使用项目目录创建。");
                fallbackToProjectDir = true;
                baseRoot = AppDomain.CurrentDomain.BaseDirectory;
            }

            if (fallbackToProjectDir)
            {
                string folderName = $"{titleWord1} {titleWord2}";
                newFolderPath = Path.Combine(baseRoot, folderName);
                int idx = 1;
                while (Directory.Exists(newFolderPath))
                {
                    newFolderPath = Path.Combine(baseRoot, $"{folderName} ({idx++})");
                }
                Directory.CreateDirectory(newFolderPath);
                Log("创建临时目标文件夹（项目目录下）: " + newFolderPath);
            }
            else
            {

                var latest = FindLatestDateInTargetFolders(targetRoot);
                if (latest == null)
                {
                    Log("⚠ 在目标根目录未找到符合格式的历史文件夹，使用当前日期作为基准。");
                    latest = DateTime.Now.Date;
                }

                DateTime newDate = latest.Value.AddDays(2);
                string newPrefix = FormatYyMmDdByRule(newDate); // 例如 421030
                string newFolderName = $"{newPrefix} {titleWord1} {titleWord2}";
                newFolderPath = Path.Combine(baseRoot, newFolderName);

                // 如果已经存在相同文件夹，则直接使用（或可选择自动改名）；这里选择在已存在时加入一个序号后缀，避免覆盖
                if (Directory.Exists(newFolderPath))
                {
                    int idx = 1;
                    string tryPath;
                    do
                    {
                        tryPath = Path.Combine(targetRoot, $"{newFolderName} ({idx})");
                        idx++;
                    } while (Directory.Exists(tryPath));
                    newFolderPath = tryPath;
                }

                Directory.CreateDirectory(newFolderPath);
                Log("创建目标文件夹: " + newFolderPath);
            }
            // 复制视频
            string destVideoName = $"{titleWord1} {titleWord2}.mp4";
            string destVideoPath = Path.Combine(newFolderPath, destVideoName);
            File.Copy(finalVideoPath, destVideoPath, true);
            Log("已复制视频到: " + destVideoPath);

            // 生成文本文件（格式：第一行 标题：xxxx xxxx；空行；正文；空行；经文；空行；简介）
            string textFileName = $"{titleWord1} {titleWord2} 文本.txt";
            string textFilePath = Path.Combine(newFolderPath, textFileName);

            var sb = new StringBuilder();
            sb.AppendLine($"标题：{titleWord1} {titleWord2}");
            sb.AppendLine();
            sb.AppendLine(TbContent.Text.Trim());
            if (!string.IsNullOrEmpty(TbVerseContent.Text))
            {
                sb.AppendLine();
                // 经文：优先使用 TbVerseContent（实际经文），如为空则使用 TbVerse（引用）
                var verse = TbVerseContent.Text.Trim();
                if (string.IsNullOrWhiteSpace(verse)) verse = TbVerse.Text.Trim();
                sb.AppendLine(verse);
            }
            if (!string.IsNullOrEmpty(TbSummary.Text.Trim()))
            {
                sb.AppendLine();
                sb.AppendLine(TbSummary.Text.Trim());
            }

            await File.WriteAllTextAsync(textFilePath, sb.ToString(), Encoding.UTF8);
            Log("已生成文本文件: " + textFilePath);

            // 打开目标文件夹
            Process.Start(new ProcessStartInfo { FileName = newFolderPath, UseShellExecute = true });
            Log("已打开目标文件夹。");
        }

        /// <summary>
        /// 在目标根目录下查找符合 yymmdd 前缀的文件夹，并解析出实际日期（根据用户规则：yy = year - 1984 + 1）
        /// 返回最近（最大的）日期；若无则返回 null。
        /// </summary>
        private DateTime? FindLatestDateInTargetFolders(string root)
        {
            try
            {
                var dirs = Directory.EnumerateDirectories(root);
                DateTime? latest = null;
                var rx = new Regex(@"^(\d{6})\s+(.+)$"); // 例如 420831 标题 标题

                foreach (var dir in dirs)
                {
                    var name = Path.GetFileName(dir);
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var m = rx.Match(name);
                    if (!m.Success) continue;

                    string prefix = m.Groups[1].Value; // 6 digits
                    if (prefix.Length != 6) continue;
                    // 取前两位为 yy（相对 1984 的序号），后四位为 mmdd
                    string yyStr = prefix.Substring(0, 2);
                    string mmStr = prefix.Substring(2, 2);
                    string ddStr = prefix.Substring(4, 2);

                    if (!int.TryParse(yyStr, out var yy)) continue;
                    if (!int.TryParse(mmStr, out var mm)) continue;
                    if (!int.TryParse(ddStr, out var dd)) continue;

                    // 根据用户规则还原年份：year = 1984 + yy - 1
                    int year = 1984 + yy - 1;
                    try
                    {
                        var dt = new DateTime(year, mm, dd);
                        if (latest == null || dt > latest) latest = dt;
                    }
                    catch
                    {
                        // 无效日期则跳过
                        continue;
                    }
                }

                return latest;
            }
            catch (Exception ex)
            {
                Log("⚠ 查找目标目录时出错: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 根据用户的 yy 规则，将 DateTime 格式化为 yymmdd，例如 2025-10-30 -> yy=42 -> 421030
        /// 规则：yy = year - 1984 + 1，格式两位；月份和日两位。
        /// </summary>
        private string FormatYyMmDdByRule(DateTime dt)
        {
            int yy = dt.Year - 1984 + 1;
            if (yy < 0) yy = 0;
            yy = yy % 100; // 保证两位
            return $"{yy:D2}{dt.Month:D2}{dt.Day:D2}";
        }
        #endregion
    }
}
