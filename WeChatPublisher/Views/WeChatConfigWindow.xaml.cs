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
        AppIcon.Set(this);
        Loaded += (_, _) => RefreshList();
    }

    private void SetStatus(string msg, bool isError = false)
    {
        TbStatusText.Text = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        TbStatusText.Foreground = isError ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Gray);
    }

    private void RefreshList()
    {
        DgAccounts.ItemsSource = null;
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
        TxtContactImage.Text = cfg.ContactImage ?? "";

        if (cfg.AppSecretEncrypted != null)
        {
            PbAppSecret.Password = cfg.AppSecretEncrypted;
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

    private static int GetIdFromSender(object sender)
    {
        if (sender is Button btn && btn.Tag != null)
        {
            if (btn.Tag is int id) return id;
            if (btn.Tag is string s && int.TryParse(s, out var sid)) return sid;
        }
        return 0;
    }

    private void BtnSwitch_Click(object sender, RoutedEventArgs e)
    {
        var id = GetIdFromSender(sender);
        if (id == 0) return;
        AppSettings.Instance.SetActiveWeChatAccount(id);
        RefreshList();
        SetStatus("已切换当前账号");
    }

    private async void BtnHealthCheck_Click(object sender, RoutedEventArgs e)
    {
        var id = GetIdFromSender(sender);
        if (id == 0) return;

        var btn = sender as Button;
        var orig = btn?.Content?.ToString();
        try
        {
            if (btn != null) { btn.IsEnabled = false; btn.Content = "检测中..."; }
            await RunHealthCheck(id);
            RefreshList();
        }
        finally
        {
            if (btn != null) { btn.IsEnabled = true; btn.Content = orig ?? "自测"; }
        }
    }

    private async void BtnHealthCheckAll_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var orig = btn?.Content?.ToString();
        try
        {
            if (btn != null) { btn.IsEnabled = false; btn.Content = "检测中..."; }
            var accounts = AppSettings.Instance.GetAllWeChatConfigs()
                .Where(a => !string.IsNullOrWhiteSpace(a.AppId)).ToList();
            foreach (var acc in accounts)
                await RunHealthCheck(acc.Id);
            RefreshList();
            SetStatus($"自测完成: {accounts.Count} 个账号");
        }
        finally
        {
            if (btn != null) { btn.IsEnabled = true; btn.Content = orig ?? "自测全部"; }
        }
    }

    private async Task RunHealthCheck(int accountId)
    {
        var cfg = AppSettings.Instance.GetAllWeChatConfigs().FirstOrDefault(c => c.Id == accountId);
        if (cfg == null) return;

        // 未完整配置的账号跳过
        if (string.IsNullOrWhiteSpace(cfg.AppId) || cfg.AppSecretEncrypted == null)
        {
            cfg.IsHealthy = 0;
            cfg.HealthMessage = "请先填写 AppID 和 AppSecret";
            cfg.LastHealthCheck = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            AppSettings.Instance.SaveWeChatConfig(cfg);
            if (_editingId == cfg.Id) UpdateHealthIndicator(cfg);
            return;
        }

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
        var id = GetIdFromSender(sender);
        if (id == 0) return;

        var cfg = AppSettings.Instance.GetAllWeChatConfigs().FirstOrDefault(c => c.Id == id);
        var name = cfg?.AccountName ?? "此账号";

        if (MessageBox.Show($"确定删除账号「{name}」?\n此操作不可恢复。", "确认删除",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            AppSettings.Instance.DeleteWeChatConfig(id);
            if (_editingId == id) { _editingId = 0; ClearForm(); }
            RefreshList();
            SetStatus($"已删除: {name}");
        }
    }

    private void BtnBrowseContact_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.gif|所有文件|*.*",
            InitialDirectory = @"F:\传道 & 公众号文案\扫码关注设计"
        };
        if (dialog.ShowDialog() == true)
            TxtContactImage.Text = dialog.FileName;
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
            ContactImage = TxtContactImage.Text.Trim(),
            IsActive = 1,
            SortOrder = _editingId == 0 ? AppSettings.Instance.GetAllWeChatConfigs().Count : 0
        };

        if (!string.IsNullOrWhiteSpace(PbAppSecret.Password))
            cfg.AppSecretEncrypted = PbAppSecret.Password.Trim();

        if (_editingId > 0)
        {
            cfg.Id = _editingId;
            cfg.IsHealthy = 0;
            cfg.LastHealthCheck = null;
            cfg.HealthMessage = null;
        }

        AppSettings.Instance.SaveWeChatConfig(cfg);
        var isUpdate = _editingId > 0;
        _editingId = 0;
        ClearForm();
        RefreshList();
        SetStatus(isUpdate ? "账号已更新" : "账号已添加");
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
