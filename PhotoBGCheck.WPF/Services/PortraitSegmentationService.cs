using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;

namespace PhotoBGCheck.WPF.Services
{
    /// <summary>
    /// MODNet 人像分割服务
    /// </summary>
    public class PortraitSegmentationService : IDisposable
    {
        private InferenceSession? _session;
        private readonly string _modelPath;
        private bool _isInitialized;

        public PortraitSegmentationService()
        {
            // 模型文件路径（将放在 Assets 目录）
            _modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "modnet.onnx");
        }

        /// <summary>
        /// 初始化模型（延迟加载，只在需要时加载）
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized || !File.Exists(_modelPath))
                return;

            try
            {
                var sessionOptions = new SessionOptions
                {
                    // 使用CPU，避免GPU依赖
                    ExecutionMode = ExecutionMode.ORT_PARALLEL
                };

                _session = new InferenceSession(_modelPath, sessionOptions);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"无法加载MODNet模型: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 判断模型是否可用
        /// </summary>
        public bool IsAvailable()
        {
            return File.Exists(_modelPath);
        }

        /// <summary>
        /// 使用MODNet分割人像，返回背景mask（true表示背景，false表示人像）
        /// </summary>
        public bool[,] SegmentPortrait(string imagePath)
        {
            if (!_isInitialized)
                Initialize();

            if (_session == null)
                throw new InvalidOperationException("MODNet模型未初始化");

            using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(imagePath);
            int originalWidth = image.Width;
            int originalHeight = image.Height;

            // MODNet 需要输入 512x512 的图像
            const int modelSize = 512;
            using var resizedImage = image.Clone(ctx => ctx.Resize(modelSize, modelSize));

            // 转换为ONNX输入格式 [1, 3, 512, 512]
            var inputTensor = new DenseTensor<float>(new[] { 1, 3, modelSize, modelSize });

            resizedImage.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < modelSize; y++)
                {
                    var pixelRow = accessor.GetRowSpan(y);
                    for (int x = 0; x < modelSize; x++)
                    {
                        var pixel = pixelRow[x];
                        // 归一化到 [0, 1] 并调整通道顺序为 CHW
                        inputTensor[0, 0, y, x] = pixel.R / 255f;
                        inputTensor[0, 1, y, x] = pixel.G / 255f;
                        inputTensor[0, 2, y, x] = pixel.B / 255f;
                    }
                }
            });

            // 运行推理
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", inputTensor)
            };

            using var results = _session.Run(inputs);
            var output = results.First().AsEnumerable<float>().ToArray();

            // 将结果调整回原始图像大小
            var mask = new bool[originalHeight, originalWidth];
            
            for (int y = 0; y < originalHeight; y++)
            {
                for (int x = 0; x < originalWidth; x++)
                {
                    // 映射到512x512的位置
                    int mappedY = (int)((float)y / originalHeight * modelSize);
                    int mappedX = (int)((float)x / originalWidth * modelSize);
                    int index = mappedY * modelSize + mappedX;

                    // alpha < 0.5 表示背景，>= 0.5 表示人像
                    mask[y, x] = output[index] < 0.5f;
                }
            }

            return mask;
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
