namespace PhotoBGCheck.WPF.Models
{
    /// <summary>
    /// 照片检测结果模型
    /// </summary>
    public class PhotoCheckResult
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public bool IsQualified { get; set; }
        public double WhitePixelPercentage { get; set; }
        public int AverageBackgroundBrightness { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
