using System.Windows;
using System.Windows.Controls;
using WeChatPublisher.Models;
using WeChatPublisher.Services;

namespace WeChatPublisher.Views;

public partial class AiConfigWindow : Window
{
    private AiConfig? _textConfig;
    private AiConfig? _imageConfig;

    public AiConfigWindow()
    {
        InitializeComponent();
        AppIcon.Set(this);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LoadConfigs();
    }

    private void LoadConfigs()
    {
        var configs = AppSettings.Instance.GetAllAiConfigs();
        _textConfig = configs.FirstOrDefault(c => c.ProviderType == "TextGeneration");
        _imageConfig = configs.FirstOrDefault(c => c.ProviderType == "ImageGeneration");

        if (_textConfig != null)
        {
            TxtTextBaseUrl.Text = _textConfig.BaseUrl;
            TxtTextModel.Text = _textConfig.ModelName;
            TxtTextMaxTokens.Text = _textConfig.DefaultMaxTokens.ToString();
            TxtTextTemperature.Text = _textConfig.DefaultTemperature.ToString();
            TxtTextSystemPrompt.Text = _textConfig.SystemPrompt ?? "";
            if (_textConfig.ApiKeyEncrypted != null)
            {
                try { PbTextApiKey.Password = ConfigEncryptionService.Decrypt(_textConfig.ApiKeyEncrypted); }
                catch { }
            }
            SelectProvider(CmbTextProvider, _textConfig.ProviderName);
        }

        if (_imageConfig != null)
        {
            TxtImageModel.Text = _imageConfig.ModelName;
            TxtImageBaseUrl.Text = _imageConfig.BaseUrl;
            if (_imageConfig.ApiKeyEncrypted != null)
            {
                try { PbImageApiKey.Password = ConfigEncryptionService.Decrypt(_imageConfig.ApiKeyEncrypted); }
                catch { }
            }
            SelectProvider(CmbImageProvider, _imageConfig.ProviderName);
            if (!string.IsNullOrWhiteSpace(_imageConfig.ImageSize))
                SelectByContent(CmbImageSize, _imageConfig.ImageSize);
            TxtImageCount.Text = _imageConfig.ImageCount.ToString();
        }
    }

    private static void SelectProvider(ComboBox cmb, string name)
    {
        foreach (ComboBoxItem item in cmb.Items)
        {
            if (item.Tag?.ToString() == name)
            {
                cmb.SelectedItem = item;
                return;
            }
        }
    }

    private static void SelectByContent(ComboBox cmb, string content)
    {
        foreach (ComboBoxItem item in cmb.Items)
        {
            if (item.Content?.ToString() == content)
            {
                cmb.SelectedItem = item;
                return;
            }
        }
    }

    private void CmbTextProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTextProvider.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            if (TxtTextBaseUrl.Text.Length == 0 || _textConfig == null)
            {
                TxtTextBaseUrl.Text = tag switch
                {
                    "DeepSeek" => "https://api.deepseek.com",
                    "OpenAI" => "https://api.openai.com",
                    _ => ""
                };
                TxtTextModel.Text = tag switch
                {
                    "DeepSeek" => "deepseek-chat",
                    "OpenAI" => "gpt-4o",
                    _ => ""
                };
            }
        }
    }

    private void CmbImageProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbImageProvider.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            if (TxtImageBaseUrl.Text.Length == 0 || _imageConfig == null)
            {
                (TxtImageBaseUrl.Text, TxtImageModel.Text) = tag switch
                {
                    "TokenHub" => ("https://tokenhub.tencentmaas.com/v1", "hy-image-v3.0"),
                    "HunyuanImage" => ("https://api.hunyuan.cloud.tencent.com/v1", "hunyuan-image-3.0-instruct"),
                    "DALLE" => ("https://api.openai.com", "dall-e-3"),
                    _ => ("", "")
                };
            }
        }
    }

    private void BtnShowHideKey_Click(object sender, RoutedEventArgs e)
    {
        // PasswordBox toggle - simplified: just show it
        if (PbTextApiKey.Password.Length > 0)
            MessageBox.Show($"当前Key: {PbTextApiKey.Password[..Math.Min(4, PbTextApiKey.Password.Length)]}...",
                "API Key", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void BtnTestTextAi_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var key = PbTextApiKey.Password;
            if (string.IsNullOrWhiteSpace(key))
            {
                MessageBox.Show("请先输入API Key", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var client = new DeepSeekClient(
                new HttpClient { Timeout = TimeSpan.FromSeconds(15) },
                key,
                TxtTextBaseUrl.Text,
                TxtTextModel.Text);

            var result = await client.ChatAsync(
                "你是一个助手，只回答OK。",
                "测试连接",
                maxTokens: 10);

            MessageBox.Show($"连接成功! 回复: {result}", "测试结果",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"连接失败: {ex.Message}", "测试结果",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnTestImageAi_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var originalContent = btn?.Content?.ToString();
        try
        {
            if (btn != null) { btn.IsEnabled = false; btn.Content = "测试中..."; }

            var apiKey = PbImageApiKey.Password;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                MessageBox.Show("请先输入图像API Key", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var provider = (CmbImageProvider.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            var model = TxtImageModel.Text.Trim();
            var baseUrl = TxtImageBaseUrl.Text.Trim();

            var service = new AIImageService();
            var imageBytes = await service.GenerateImageAsync(
                prompt: "生成一张温馨的测试图片：一朵盛开的玫瑰花",
                provider: provider,
                baseUrl: baseUrl,
                model: model,
                apiKey: apiKey);

            var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "Images");
            Directory.CreateDirectory(outputDir);
            var filePath = Path.Combine(outputDir, $"test_{DateTime.Now:yyyyMMddHHmmss}.png");
            await File.WriteAllBytesAsync(filePath, imageBytes);

            MessageBox.Show($"图像生成成功!\n保存至: {filePath}", "测试成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"图像生成失败: {ex.Message}", "测试失败",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (btn != null) { btn.IsEnabled = true; btn.Content = originalContent ?? "测试"; }
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Save text config
            if (_textConfig == null)
            {
                _textConfig = new AiConfig { ProviderType = "TextGeneration", IsActive = 1 };
            }

            _textConfig.ProviderName = (CmbTextProvider.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "DeepSeek";
            _textConfig.BaseUrl = TxtTextBaseUrl.Text.Trim();
            _textConfig.ModelName = TxtTextModel.Text.Trim();
            _textConfig.DefaultMaxTokens = int.TryParse(TxtTextMaxTokens.Text, out var mt) ? mt : 4096;
            _textConfig.DefaultTemperature = double.TryParse(TxtTextTemperature.Text, out var t) ? t : 0.7;
            _textConfig.SystemPrompt = TxtTextSystemPrompt.Text.Trim();

            if (!string.IsNullOrWhiteSpace(PbTextApiKey.Password))
                _textConfig.ApiKeyEncrypted = ConfigEncryptionService.Encrypt(PbTextApiKey.Password);

            AppSettings.Instance.SaveAiConfig(_textConfig);

            // Save image config
            if (_imageConfig == null)
            {
                _imageConfig = new AiConfig { ProviderType = "ImageGeneration", IsActive = 1 };
            }

            _imageConfig.ProviderName = (CmbImageProvider.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "DALLE";
            _imageConfig.BaseUrl = TxtImageBaseUrl.Text.Trim();
            _imageConfig.ModelName = TxtImageModel.Text.Trim();
            _imageConfig.ImageSize = (CmbImageSize.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "1024x1024";
            _imageConfig.ImageCount = int.TryParse(TxtImageCount.Text, out var ic) ? Math.Max(1, ic) : 1;

            if (!string.IsNullOrWhiteSpace(PbImageApiKey.Password))
                _imageConfig.ApiKeyEncrypted = ConfigEncryptionService.Encrypt(PbImageApiKey.Password);

            AppSettings.Instance.SaveAiConfig(_imageConfig);

            MessageBox.Show("配置已保存", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
