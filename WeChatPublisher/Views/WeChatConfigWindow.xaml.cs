using System.Windows;
using WeChatPublisher.Models;
using WeChatPublisher.Services;

namespace WeChatPublisher.Views;

public partial class WeChatConfigWindow : Window
{
    private WeChatConfig? _config;

    public WeChatConfigWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _config = AppSettings.Instance.GetActiveWeChatConfig();
        if (_config != null)
        {
            TxtAppId.Text = _config.AppId;
            TxtApiBaseUrl.Text = _config.ApiBaseUrl;
            CbPublishAsDraft.IsChecked = _config.PublishAsDraft == 1;
            CbAutoSanitize.IsChecked = _config.AutoSanitize == 1;
            TxtDefaultTags.Text = _config.DefaultTags ?? "";

            if (_config.AppSecretEncrypted != null)
            {
                try { PbAppSecret.Password = ConfigEncryptionService.Decrypt(_config.AppSecretEncrypted); }
                catch { }
            }
        }
    }

    private async void BtnVerify_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var service = new WeChatService();
            var token = await service.GetAccessTokenAsync();
            MessageBox.Show($"验证成功! AccessToken: {token[..Math.Min(16, token.Length)]}...",
                "验证结果", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"验证失败: {ex.Message}", "验证结果",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnShowSecret_Click(object sender, RoutedEventArgs e)
    {
        if (PbAppSecret.Password.Length > 0)
            MessageBox.Show($"当前Secret前4位: {PbAppSecret.Password[..Math.Min(4, PbAppSecret.Password.Length)]}...",
                "AppSecret", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnBrowseImageFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            TxtImageFolder.Text = dialog.SelectedPath;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(TxtAppId.Text))
            {
                MessageBox.Show("请输入AppID", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _config ??= new WeChatConfig { IsActive = 1 };

            _config.AppId = TxtAppId.Text.Trim();
            _config.ApiBaseUrl = TxtApiBaseUrl.Text.Trim();
            _config.PublishAsDraft = CbPublishAsDraft.IsChecked == true ? 1 : 0;
            _config.AutoSanitize = CbAutoSanitize.IsChecked == true ? 1 : 0;
            _config.DefaultTags = TxtDefaultTags.Text.Trim();

            if (!string.IsNullOrWhiteSpace(PbAppSecret.Password))
                _config.AppSecretEncrypted = ConfigEncryptionService.Encrypt(PbAppSecret.Password);

            AppSettings.Instance.SaveWeChatConfig(_config);

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
