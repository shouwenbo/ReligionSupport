using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using PhotoBGCheck.WPF.Models;
using System.IO;

namespace PhotoBGCheck.WPF.Services
{
    /// <summary>
    /// 照片背景检测服务
    /// </summary>
    public class PhotoBackgroundCheckService : IDisposable
    {
        private readonly int _whiteThreshold;
        private readonly double _whitePixelPercentageThreshold;
        private readonly PortraitSegmentationService? _segmentationService;

        public PhotoBackgroundCheckService(int whiteThreshold = 210, double whitePixelPercentageThreshold = 0.75)
        {
            _whiteThreshold = whiteThreshold;
            _whitePixelPercentageThreshold = whitePixelPercentageThreshold;
            
            // 初始化人像分割服务（仅在模型可用时）
            _segmentationService = new PortraitSegmentationService();
        }

        /// <summary>
        /// 检查AI模式是否可用
        /// </summary>
        public bool IsAIModeAvailable()
        {
            return _segmentationService?.IsAvailable() ?? false;
        }

        /// <summary>
        /// 检测单张照片的背景是否为合格的白底
        /// </summary>
        public PhotoCheckResult CheckPhoto(string filePath, DetectionMode mode = DetectionMode.Fast)
        {
            var result = new PhotoCheckResult
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath)
            };

            try
            {
                if (mode == DetectionMode.AI && IsAIModeAvailable())
                {
                    return CheckPhotoWithAI(filePath, result);
                }
                else
                {
                    return CheckPhotoFast(filePath, result);
                }
            }
            catch (Exception ex)
            {
                result.IsQualified = false;
                result.ErrorMessage = $"检测失败: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 快速模式 - 检测上方角落 + 肤色过滤
        /// </summary>
        private PhotoCheckResult CheckPhotoFast(string filePath, PhotoCheckResult result)
        {
            try
            {
                using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(filePath);
                
                int whitePixels = 0;
                long totalBrightness = 0;
                int backgroundPixels = 0;
                
                // 使用上方双角检测法：只检测左上角和右上角区域（证件照下方有肩膀/西装）
                // 每个角落检测 20% x 20% 的区域，确保有足够的采样点
                int cornerWidth = (int)(image.Width * 0.20);
                int cornerHeight = (int)(image.Height * 0.20);

                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        var pixelRow = accessor.GetRowSpan(y);
                        
                        for (int x = 0; x < pixelRow.Length; x++)
                        {
                            // 只检测左上角和右上角区域（避开下方的肩膀、西装等）
                            bool isTopLeft = x < cornerWidth && y < cornerHeight;
                            bool isTopRight = x >= image.Width - cornerWidth && y < cornerHeight;
                            
                            bool isBackground = isTopLeft || isTopRight;

                            if (isBackground)
                            {
                                var pixel = pixelRow[x];
                                int brightness = (pixel.R + pixel.G + pixel.B) / 3;
                                
                                // 简单的肤色/深色过滤：排除可能是人像的像素
                                // 肤色范围大致为：R > G > B，且不是纯白
                                bool likelySkin = pixel.R > 95 && pixel.G > 40 && pixel.B > 20 &&
                                                 pixel.R > pixel.G && pixel.G > pixel.B &&
                                                 pixel.R - pixel.G > 15 && brightness < 240;
                                
                                // 深色（头发、西装）：整体亮度很低
                                bool likelyDark = brightness < 80;
                                
                                // 只统计既不是肤色也不是深色的像素（纯背景）
                                if (!likelySkin && !likelyDark)
                                {
                                    totalBrightness += brightness;
                                    backgroundPixels++;

                                    // 检测是否为白色像素（RGB值都很高且相近）
                                    if (pixel.R >= _whiteThreshold && 
                                        pixel.G >= _whiteThreshold && 
                                        pixel.B >= _whiteThreshold &&
                                        Math.Abs(pixel.R - pixel.G) < 15 &&
                                        Math.Abs(pixel.G - pixel.B) < 15 &&
                                        Math.Abs(pixel.R - pixel.B) < 15)
                                    {
                                        whitePixels++;
                                    }
                                }
                            }
                        }
                    }
                });

                // 计算白色像素占背景的比例
                double whitePercentage = backgroundPixels > 0 
                    ? (double)whitePixels / backgroundPixels 
                    : 0;
                
                int avgBrightness = backgroundPixels > 0 
                    ? (int)(totalBrightness / backgroundPixels) 
                    : 0;

                result.WhitePixelPercentage = whitePercentage;
                result.AverageBackgroundBrightness = avgBrightness;
                
                // 判断是否合格：白色像素比例足够高，且平均亮度足够高
                result.IsQualified = whitePercentage >= _whitePixelPercentageThreshold && 
                                    avgBrightness >= _whiteThreshold;
                
                return result;
            }
            catch (Exception ex)
            {
                result.IsQualified = false;
                result.ErrorMessage = $"检测失败: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// AI智能模式 - MODNet人像分割 + 全背景检测
        /// </summary>
        private PhotoCheckResult CheckPhotoWithAI(string filePath, PhotoCheckResult result)
        {
            if (_segmentationService == null)
            {
                result.ErrorMessage = "AI模型未初始化";
                return result;
            }

            using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(filePath);
            
            // 使用MODNet分割人像
            var backgroundMask = _segmentationService.SegmentPortrait(filePath);
            
            int whitePixels = 0;
            long totalBrightness = 0;
            int backgroundPixels = 0;

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var pixelRow = accessor.GetRowSpan(y);
                    
                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        // 只检测背景区域（mask为true表示背景）
                        if (backgroundMask[y, x])
                        {
                            var pixel = pixelRow[x];
                            int brightness = (pixel.R + pixel.G + pixel.B) / 3;
                            totalBrightness += brightness;
                            backgroundPixels++;

                            // 检测是否为白色像素
                            if (pixel.R >= _whiteThreshold && 
                                pixel.G >= _whiteThreshold && 
                                pixel.B >= _whiteThreshold &&
                                Math.Abs(pixel.R - pixel.G) < 15 &&
                                Math.Abs(pixel.G - pixel.B) < 15 &&
                                Math.Abs(pixel.R - pixel.B) < 15)
                            {
                                whitePixels++;
                            }
                        }
                    }
                }
            });

            // 计算统计数据
            double whitePercentage = backgroundPixels > 0 
                ? (double)whitePixels / backgroundPixels 
                : 0;
            
            int avgBrightness = backgroundPixels > 0 
                ? (int)(totalBrightness / backgroundPixels) 
                : 0;

            result.WhitePixelPercentage = whitePercentage;
            result.AverageBackgroundBrightness = avgBrightness;
            result.IsQualified = whitePercentage >= _whitePixelPercentageThreshold && 
                                avgBrightness >= _whiteThreshold;

            return result;
        }

        /// <summary>
        /// 批量检测照片
        /// </summary>
        public async Task<List<PhotoCheckResult>> CheckPhotosAsync(
            IEnumerable<string> filePaths, 
            DetectionMode mode = DetectionMode.Fast,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var results = new List<PhotoCheckResult>();
            var fileList = filePaths.ToList();
            int totalFiles = fileList.Count;
            int processedFiles = 0;

            await Task.Run(() =>
            {
                foreach (var filePath in fileList)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var result = CheckPhoto(filePath, mode);
                    results.Add(result);

                    processedFiles++;
                    progress?.Report((int)((double)processedFiles / totalFiles * 100));
                }
            }, cancellationToken);

            return results;
        }

        public void Dispose()
        {
            _segmentationService?.Dispose();
        }
    }
}
