using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using WeChatPublisher.Models;

namespace WeChatPublisher.Views;

public partial class TtsConfigWindow : Window
{
    private TtsApiConfig? _ttsConfig;
    private TtsApiConfig? _subConfig;

    public TtsConfigWindow()
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
        var configs = AppSettings.Instance.GetAllTtsConfigs();
        _ttsConfig = configs.FirstOrDefault(c => c.ConfigType == "TTS");
        _subConfig = configs.FirstOrDefault(c => c.ConfigType == "Subtitle");

        if (_ttsConfig != null) LoadTtsToUi(_ttsConfig);
        if (_subConfig != null) LoadSubToUi(_subConfig);
    }

    private void LoadTtsToUi(TtsApiConfig cfg)
    {
        TxtTtsBaseUrl.Text = cfg.BaseUrl ?? "";
        TxtTtsTokenUrl.Text = cfg.TokenFetchUrl ?? "";
        TxtTtsTokenRegex.Text = cfg.TokenRegexPattern ?? "";
        TxtTtsEndpoint.Text = cfg.GenerateEndpoint ?? "";
        TxtTtsSuccessField.Text = cfg.SuccessCodeField;
        TxtTtsSuccessValue.Text = cfg.SuccessCodeValue;
        SelectTag(CmbTtsType, cfg.ApiType);

        if (cfg.DefaultParamsJSON != null)
        {
            try
            {
                var p = JsonSerializer.Deserialize<Dictionary<string, string>>(cfg.DefaultParamsJSON);
                if (p != null)
                {
                    TxtTtsLanguage.Text = p.GetValueOrDefault("language", "");
                    TxtTtsVoice.Text = p.GetValueOrDefault("voice", "");
                    TxtTtsRate.Text = p.GetValueOrDefault("rate", "");
                    TxtTtsPitch.Text = p.GetValueOrDefault("pitch", "");
                    TxtTtsBitrate.Text = p.GetValueOrDefault("kbitrate", "");
                    TxtTtsExtraParams.Text = cfg.DefaultParamsJSON;
                }
            }
            catch { }
        }
    }

    private void LoadSubToUi(TtsApiConfig cfg)
    {
        TxtSubBaseUrl.Text = cfg.BaseUrl ?? "";
        TxtSubTokenUrl.Text = cfg.TokenFetchUrl ?? "";
        TxtSubTokenRegex.Text = cfg.TokenRegexPattern ?? "";
        TxtSubEndpoint.Text = cfg.GenerateEndpoint ?? "";
        TxtSubLanguage.Text = "zh-CN";
        SelectTag(CmbSubType, cfg.ApiType);
    }

    private static void SelectTag(ComboBox cmb, string tag)
    {
        foreach (ComboBoxItem item in cmb.Items)
            if (item.Tag?.ToString() == tag) { cmb.SelectedItem = item; return; }
    }

    private void BtnTestTts_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("TTS测试功能需要有效的API配置和音频播放器", "提示",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnTestSub_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("字幕测试功能需要有效的API配置和音频文件", "提示",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var ttsParams = new Dictionary<string, string>
            {
                ["language"] = TxtTtsLanguage.Text,
                ["voice"] = TxtTtsVoice.Text,
                ["rate"] = TxtTtsRate.Text,
                ["pitch"] = TxtTtsPitch.Text,
                ["kbitrate"] = TxtTtsBitrate.Text
            };

            if (_ttsConfig == null)
            {
                _ttsConfig = new TtsApiConfig { ConfigType = "TTS", IsActive = 1 };
            }

            _ttsConfig.ApiType = (CmbTtsType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "WebScrape";
            _ttsConfig.BaseUrl = TxtTtsBaseUrl.Text.Trim();
            _ttsConfig.TokenFetchUrl = TxtTtsTokenUrl.Text.Trim();
            _ttsConfig.TokenRegexPattern = TxtTtsTokenRegex.Text.Trim();
            _ttsConfig.GenerateEndpoint = TxtTtsEndpoint.Text.Trim();
            _ttsConfig.SuccessCodeField = TxtTtsSuccessField.Text.Trim();
            _ttsConfig.SuccessCodeValue = TxtTtsSuccessValue.Text.Trim();
            _ttsConfig.DefaultParamsJSON = JsonSerializer.Serialize(ttsParams);

            AppSettings.Instance.SaveTtsApiConfig(_ttsConfig);

            if (_subConfig == null)
            {
                _subConfig = new TtsApiConfig { ConfigType = "Subtitle", IsActive = 1 };
            }

            _subConfig.ApiType = (CmbSubType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "WebScrape";
            _subConfig.BaseUrl = TxtSubBaseUrl.Text.Trim();
            _subConfig.TokenFetchUrl = TxtSubTokenUrl.Text.Trim();
            _subConfig.TokenRegexPattern = TxtSubTokenRegex.Text.Trim();
            _subConfig.GenerateEndpoint = TxtSubEndpoint.Text.Trim();

            AppSettings.Instance.SaveTtsApiConfig(_subConfig);

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
