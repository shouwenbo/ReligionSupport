namespace PhotoBGCheck.WPF.Models
{
    /// <summary>
    /// 背景检测模式
    /// </summary>
    public enum DetectionMode
    {
        /// <summary>
        /// 快速模式 - 检测上方角落 + 肤色过滤（适合大批量快速筛查）
        /// </summary>
        Fast = 0,

        /// <summary>
        /// AI智能模式 - MODNet人像分割 + 全背景检测（推荐，准确率更高）
        /// </summary>
        AI = 1
    }
}
