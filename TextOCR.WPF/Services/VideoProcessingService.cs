using OpenCvSharp;
using TextOCR.WPF.Models;

namespace TextOCR.WPF.Services;

public interface IVideoProcessingService
{
    VideoInfo GetVideoInfo(string videoPath);
    PreviewResult ExtractFrame(string videoPath, int second, int subtitleHeight, bool fromTop);
    byte[] MatToImageBytes(Mat mat);
}

public class VideoProcessingService : IVideoProcessingService
{
    public VideoInfo GetVideoInfo(string videoPath)
    {
        using var cap = new VideoCapture(videoPath);
        
        if (!cap.IsOpened())
            throw new InvalidOperationException("无法打开视频文件");

        var info = new VideoInfo
        {
            Width = (int)cap.Get(VideoCaptureProperties.FrameWidth),
            Height = (int)cap.Get(VideoCaptureProperties.FrameHeight),
            Fps = cap.Get(VideoCaptureProperties.Fps),
            TotalFrames = (int)cap.Get(VideoCaptureProperties.FrameCount)
        };

        info.Duration = TimeSpan.FromSeconds(info.TotalFrames / info.Fps);

        return info;
    }

    public PreviewResult ExtractFrame(string videoPath, int second, int subtitleHeight, bool fromTop)
    {
        using var cap = new VideoCapture(videoPath);
        
        if (!cap.IsOpened())
            throw new InvalidOperationException("无法打开视频文件");

        var fps = (int)cap.Get(VideoCaptureProperties.Fps);
        var width = (int)cap.Get(VideoCaptureProperties.FrameWidth);
        var height = (int)cap.Get(VideoCaptureProperties.FrameHeight);
        var frameNumber = fps * second;

        cap.Set(VideoCaptureProperties.PosFrames, frameNumber);

        using var frame = new Mat();
        if (!cap.Read(frame) || frame.Empty())
            throw new InvalidOperationException("无法读取视频帧");

        // 提取字幕区域
        using var subtitleRegion = fromTop
            ? new Mat(frame, new Rect(0, 0, width, subtitleHeight))
            : new Mat(frame, new Rect(0, height - subtitleHeight, width, subtitleHeight));

        // 图像预处理以提高OCR识别精度
        using var processed = PreprocessForOCR(subtitleRegion);

        return new PreviewResult
        {
            FrameImage = MatToImageBytes(frame),
            SubtitleRegionImage = MatToImageBytes(processed)
        };
    }

    private Mat PreprocessForOCR(Mat input)
    {
        // 1. 放大图像（2倍），OCR对大图识别更准确
        var enlarged = new Mat();
        Cv2.Resize(input, enlarged, new OpenCvSharp.Size(), 2.0, 2.0, InterpolationFlags.Cubic);

        // 2. 转为灰度图
        var gray = new Mat();
        if (enlarged.Channels() == 3)
        {
            Cv2.CvtColor(enlarged, gray, ColorConversionCodes.BGR2GRAY);
        }
        else
        {
            gray = enlarged.Clone();
        }
        enlarged.Dispose();

        // 3. 去噪（保留文字边缘）
        var denoised = new Mat();
        Cv2.FastNlMeansDenoising(gray, denoised, 10, 7, 21);
        gray.Dispose();

        // 4. 增强对比度（CLAHE - 对比度受限自适应直方图均衡化）
        var clahe = Cv2.CreateCLAHE(clipLimit: 2.0, tileGridSize: new OpenCvSharp.Size(8, 8));
        var enhanced = new Mat();
        clahe.Apply(denoised, enhanced);
        denoised.Dispose();

        // 5. 锐化（可选 - 增强文字边缘）
        var kernel = new Mat(3, 3, MatType.CV_32F, new float[]
        {
            0, -1, 0,
            -1, 5, -1,
            0, -1, 0
        });
        var sharpened = new Mat();
        Cv2.Filter2D(enhanced, sharpened, -1, kernel);
        enhanced.Dispose();
        kernel.Dispose();

        return sharpened;
    }

    public byte[] MatToImageBytes(Mat mat)
    {
        return mat.ToBytes(".png");
    }
}
