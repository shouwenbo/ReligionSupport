using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoMediaConvert.WPF
{
    public class FfmpegService
    {
        private readonly string _ffmpegPath;
        public FfmpegService(string ffmpegPath) => _ffmpegPath = ffmpegPath;

        private static readonly Regex _durationRx = new Regex(@"Duration:\s(?<h>\d{2}):(?<m>\d{2}):(?<s>\d{2}\.\d+)", RegexOptions.Compiled);
        private static readonly Regex _timeRx = new Regex(@"time=(?<h>\d{2}):(?<m>\d{2}):(?<s>\d{2}\.\d+)", RegexOptions.Compiled);

        public async Task ConvertWithProgressAsync(string inputFile, string outputFile, Action<double> progressCallback, CancellationToken token)
        {
            // First run ffmpeg to get duration (parse stderr)
            var duration = await GetDurationAsync(inputFile, token);

            var args = $"-y -i \"{inputFile}\" -q:a 0 -map a \"{outputFile}\"";

            var psi = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };

            using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };

            var tcs = new TaskCompletionSource<bool>();

            p.ErrorDataReceived += (s, e) =>
            {
                if (e.Data == null) return;

                var m = _timeRx.Match(e.Data);
                if (m.Success && duration > TimeSpan.Zero)
                {
                    var current = ParseTime(m);
                    var ratio = Math.Min(1.0, current.TotalSeconds / duration.TotalSeconds);
                    progressCallback(ratio);
                }
            };

            p.Exited += (s, e) => tcs.TrySetResult(true);

            p.Start();
            p.BeginErrorReadLine();

            using (token.Register(() =>
            {
                try { if (!p.HasExited) p.Kill(); } catch { }
            }))
            {
                await tcs.Task;
            }
        }

        private static TimeSpan ParseTime(Match m)
        {
            int h = int.Parse(m.Groups["h"].Value);
            int mm = int.Parse(m.Groups["m"].Value);
            double s = double.Parse(m.Groups["s"].Value);
            return new TimeSpan(0, h, mm, 0).Add(TimeSpan.FromSeconds(s));
        }

        private async Task<TimeSpan> GetDurationAsync(string inputFile, CancellationToken token)
        {
            var args = $"-i \"{inputFile}\"";
            var psi = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var p = Process.Start(psi);
            var stderr = await p.StandardError.ReadToEndAsync();
            p.WaitForExit();

            var m = _durationRx.Match(stderr);
            if (m.Success)
            {
                int h = int.Parse(m.Groups["h"].Value);
                int mm = int.Parse(m.Groups["m"].Value);
                double s = double.Parse(m.Groups["s"].Value);
                return new TimeSpan(0, h, mm, 0).Add(TimeSpan.FromSeconds(s));
            }

            return TimeSpan.Zero;
        }
    }
}
