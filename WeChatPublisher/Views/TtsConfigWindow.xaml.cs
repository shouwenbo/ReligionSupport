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

    private async void BtnTestTts_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var orig = btn?.Content?.ToString();
        try
        {
            if (btn != null) { btn.IsEnabled = false; btn.Content = "测试中..."; }

            var config = BuildTtsConfigFromUi();
            var service = new Services.TtsService(config);
            var testText = "你好，这是一条语音合成测试。愿你今日平安喜乐。";

            var tempDir = Path.Combine(Path.GetTempPath(), "WeChatPublisher");
            Directory.CreateDirectory(tempDir);
            var outputPath = Path.Combine(tempDir, "tts_test.mp3");

            var result = await service.GenerateAudioAsync(testText, outputPath);
            if (result == null)
            {
                MessageBox.Show("语音合成失败，请检查API配置是否正确", "测试失败",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Play the generated audio
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = result,
                UseShellExecute = true
            });

            MessageBox.Show($"语音合成成功!\n文件: {result}", "测试成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"TTS测试失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (btn != null) { btn.IsEnabled = true; btn.Content = orig ?? "测试朗读一段"; }
        }
    }

    private TtsApiConfig BuildTtsConfigFromUi()
    {
        return new TtsApiConfig
        {
            ConfigType = "TTS",
            ApiType = (CmbTtsType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "WebScrape",
            BaseUrl = TxtTtsBaseUrl.Text.Trim(),
            TokenFetchUrl = TxtTtsTokenUrl.Text.Trim(),
            TokenRegexPattern = TxtTtsTokenRegex.Text.Trim(),
            GenerateEndpoint = TxtTtsEndpoint.Text.Trim(),
            SuccessCodeField = TxtTtsSuccessField.Text.Trim(),
            SuccessCodeValue = TxtTtsSuccessValue.Text.Trim(),
            DefaultParamsJSON = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["language"] = TxtTtsLanguage.Text,
                ["voice"] = TxtTtsVoice.Text,
                ["rate"] = TxtTtsRate.Text,
                ["pitch"] = TxtTtsPitch.Text,
                ["kbitrate"] = TxtTtsBitrate.Text
            })
        };
    }

    private async void BtnTestSub_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var orig = btn?.Content?.ToString();
        try
        {
            if (btn != null) { btn.IsEnabled = false; btn.Content = "测试中..."; }

            // Need an audio file first - generate one with TTS
            var ttsConfig = BuildTtsConfigFromUi();
            var tts = new Services.TtsService(ttsConfig);
            var tempDir = Path.Combine(Path.GetTempPath(), "WeChatPublisher");
            Directory.CreateDirectory(tempDir);
            var audioPath = Path.Combine(tempDir, "sub_test.mp3");

            var audioResult = await tts.GenerateAudioAsync("测试字幕生成。今天天气真好。", audioPath);
            if (audioResult == null)
            {
                MessageBox.Show("语音生成失败，无法测试字幕", "测试失败",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var subConfig = new TtsApiConfig
            {
                ConfigType = "Subtitle",
                ApiType = (CmbSubType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "WebScrape",
                BaseUrl = TxtSubBaseUrl.Text.Trim(),
                TokenFetchUrl = TxtSubTokenUrl.Text.Trim(),
                TokenRegexPattern = TxtSubTokenRegex.Text.Trim(),
                GenerateEndpoint = TxtSubEndpoint.Text.Trim(),
                SuccessCodeField = "code",
                SuccessCodeValue = "200",
                DefaultParamsJSON = $"{{\"language\":\"{TxtSubLanguage.Text}\"}}"
            };
            var sub = new Services.SubtitleService(subConfig);
            var srtPath = await sub.GenerateSubtitleAsync(audioResult);

            if (srtPath != null && File.Exists(srtPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "notepad.exe",
                    Arguments = srtPath,
                    UseShellExecute = false
                });
                MessageBox.Show($"字幕生成成功!\n{Path.GetFileName(srtPath)}", "测试成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("字幕生成失败", "测试失败",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"字幕测试失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (btn != null) { btn.IsEnabled = true; btn.Content = orig ?? "测试字幕生成"; }
        }
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
