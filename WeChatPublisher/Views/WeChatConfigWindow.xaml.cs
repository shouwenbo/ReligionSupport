using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using WeChatPublisher.Models;
using WeChatPublisher.Services;

namespace WeChatPublisher.Views;

public partial class WeChatConfigWindow : Window
{
    private int _editingId;

    public WeChatConfigWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshList();
    }

    private void RefreshList()
    {
        DgAccounts.ItemsSource = AppSettings.Instance.GetAllWeChatConfigs();
    }

    private void DgAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgAccounts.SelectedItem is not WeChatConfig cfg) return;
        _editingId = cfg.Id;
        TxtAccountName.Text = cfg.AccountName;
        TxtAppId.Text = cfg.AppId;
        CbPublishAsDraft.IsChecked = cfg.PublishAsDraft == 1;
        CbAutoSanitize.IsChecked = cfg.AutoSanitize == 1;
        TxtDefaultTags.Text = cfg.DefaultTags ?? "";

        if (cfg.AppSecretEncrypted != null)
        {
            try { PbAppSecret.Password = ConfigEncryptionService.Decrypt(cfg.AppSecretEncrypted); }
            catch { }
        }

        foreach (ComboBoxItem item in CmbPlatform.Items)
        {
            if (item.Tag?.ToString() == cfg.Platform) { CmbPlatform.SelectedItem = item; break; }
        }

        UpdateHealthIndicator(cfg);
    }

    private void UpdateHealthIndicator(WeChatConfig cfg)
    {
        ElHealth.Fill = cfg.IsHealthy == 1
            ? new SolidColorBrush(Colors.LimeGreen)
            : new SolidColorBrush(Colors.Gray);
        TbHealthMsg.Text = cfg.IsHealthy == 1 ? (cfg.HealthMessage ?? "正常") : (cfg.HealthMessage ?? "未检测");
        TbHealthTime.Text = cfg.LastHealthCheck ?? "";
    }

    private void BtnSwitch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string idStr && int.TryParse(idStr, out var id))
        {
            AppSettings.Instance.SetActiveWeChatAccount(id);
            RefreshList();
        }
    }

    private async void BtnHealthCheck_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string idStr && int.TryParse(idStr, out var id))
        {
            await RunHealthCheck(id);
            RefreshList();
        }
    }

    private async void BtnHealthCheckAll_Click(object sender, RoutedEventArgs e)
    {
        var accounts = AppSettings.Instance.GetAllWeChatConfigs();
        foreach (var acc in accounts)
        {
            if (!string.IsNullOrWhiteSpace(acc.AppId))
                await RunHealthCheck(acc.Id);
        }
        RefreshList();
    }

    private async Task RunHealthCheck(int accountId)
    {
        var cfg = AppSettings.Instance.GetAllWeChatConfigs().FirstOrDefault(c => c.Id == accountId);
        if (cfg == null) return;

        try
        {
            var service = new WeChatService();
            await service.GetAccessTokenAsync(cfg);
            cfg.IsHealthy = 1;
            cfg.HealthMessage = "凭证有效";
        }
        catch (Exception ex)
        {
            cfg.IsHealthy = 0;
            cfg.HealthMessage = ex.Message.Length > 80 ? ex.Message[..80] : ex.Message;
        }

        cfg.LastHealthCheck = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        AppSettings.Instance.SaveWeChatConfig(cfg);

        if (_editingId == cfg.Id) UpdateHealthIndicator(cfg);
    }

    private void BtnDeleteAccount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string idStr && int.TryParse(idStr, out var id))
        {
            if (MessageBox.Show("确定删除此账号?", "确认", MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                AppSettings.Instance.DeleteWeChatConfig(id);
                RefreshList();
            }
        }
    }

    private void BtnShowSecret_Click(object sender, RoutedEventArgs e)
    {
        if (PbAppSecret.Password.Length > 0)
            MessageBox.Show($"Secret前4位: {PbAppSecret.Password[..Math.Min(4, PbAppSecret.Password.Length)]}...",
                "AppSecret", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnScanQr_Click(object sender, RoutedEventArgs e)
    {
        // Open WeChat Official Account admin page in browser for QR login
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://mp.weixin.qq.com/",
            UseShellExecute = true
        });
        MessageBox.Show("请在浏览器中扫码登录公众号后台。\n登录后进入「设置与开发」→「基本配置」查看 AppID 和 AppSecret。",
            "扫码登录", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnOpenAdmin_Click(object sender, RoutedEventArgs e)
    {
        var appId = TxtAppId.Text.Trim();
        if (!string.IsNullOrWhiteSpace(appId))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"https://mp.weixin.qq.com/advanced/advanced?action=dev&appid={appId}",
                UseShellExecute = true
            });
        }
        else
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://mp.weixin.qq.com/",
                UseShellExecute = true
            });
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtAccountName.Text))
        {
            MessageBox.Show("请输入账号名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var cfg = new WeChatConfig
        {
            AccountName = TxtAccountName.Text.Trim(),
            Platform = (CmbPlatform.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "OfficialAccount",
            AppId = TxtAppId.Text.Trim(),
            ApiBaseUrl = "https://api.weixin.qq.com",
            PublishAsDraft = CbPublishAsDraft.IsChecked == true ? 1 : 0,
            AutoSanitize = CbAutoSanitize.IsChecked == true ? 1 : 0,
            DefaultTags = TxtDefaultTags.Text.Trim(),
            IsActive = 1,
            SortOrder = _editingId == 0 ? AppSettings.Instance.GetAllWeChatConfigs().Count : 0
        };

        if (!string.IsNullOrWhiteSpace(PbAppSecret.Password))
            cfg.AppSecretEncrypted = ConfigEncryptionService.Encrypt(PbAppSecret.Password);

        if (_editingId > 0)
        {
            cfg.Id = _editingId;
            cfg.IsHealthy = 0;
            cfg.LastHealthCheck = null;
            cfg.HealthMessage = null;
        }

        AppSettings.Instance.SaveWeChatConfig(cfg);
        _editingId = 0;
        ClearForm();
        RefreshList();
        MessageBox.Show("账号已保存", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();

    private void ClearForm()
    {
        TxtAccountName.Clear();
        TxtAppId.Clear();
        PbAppSecret.Clear();
        TxtDefaultTags.Clear();
        CbPublishAsDraft.IsChecked = true;
        CbAutoSanitize.IsChecked = true;
        ElHealth.Fill = new SolidColorBrush(Colors.Gray);
        TbHealthMsg.Text = "未检测";
        TbHealthTime.Text = "";
    }
}
