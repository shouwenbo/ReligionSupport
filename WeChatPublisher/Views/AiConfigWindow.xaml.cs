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
                    "HunyuanImage" => ("https://api.hunyuan.cloud.tencent.com/v1", "hunyuan-image-3.0-instruct"),
                    "DALLE" => ("https://api.openai.com", "dall-e-3"),
                    "StableDiffusion" => ("http://localhost:7860", "stable-diffusion"),
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
        MessageBox.Show("图像AI测试功能将在Phase 5实现", "提示",
            MessageBoxButton.OK, MessageBoxImage.Information);
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
            _imageConfig.ModelName = _imageConfig.ProviderName;

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
