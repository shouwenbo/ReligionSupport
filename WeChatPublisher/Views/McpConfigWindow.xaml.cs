using System.Windows;
using System.Windows.Controls;
using WeChatPublisher.Models;
using WeChatPublisher.Services;

namespace WeChatPublisher.Views;

public partial class McpConfigWindow : Window
{
    private readonly McpService _mcpService = new();
    private int _editingId;

    public McpConfigWindow()
    {
        InitializeComponent();
        AppIcon.Set(this);
        Loaded += (_, _) => RefreshList();
    }

    private void RefreshList()
    {
        DgResources.ItemsSource = _mcpService.GetAllResources();
    }

    private void DgResources_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgResources.SelectedItem is not McpResourceConfig res) return;
        _editingId = res.Id;
        TxtResName.Text = res.Name;
        TxtResPath.Text = res.Path;
        TxtResFilter.Text = res.FileFilter ?? "*.*";
        TxtExcludeFolders.Text = res.ExcludeFolders ?? "";
        SelectTag(CmbResType, res.ResourceType);
    }

    private static void SelectTag(ComboBox cmb, string tag)
    {
        foreach (ComboBoxItem item in cmb.Items)
            if (item.Tag?.ToString() == tag) { cmb.SelectedItem = item; return; }
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

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        var id = GetIdFromSender(sender);
        if (id == 0) return;
        var res = _mcpService.GetAllResources().FirstOrDefault(r => r.Id == id);
        if (res == null) return;
        _editingId = res.Id;
        TxtResName.Text = res.Name;
        TxtResPath.Text = res.Path;
        TxtResFilter.Text = res.FileFilter ?? "*.*";
        TxtExcludeFolders.Text = res.ExcludeFolders ?? "";
        SelectTag(CmbResType, res.ResourceType);
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        var id = GetIdFromSender(sender);
        if (id == 0) return;
        var res = _mcpService.GetAllResources().FirstOrDefault(r => r.Id == id);
        var name = res?.Name ?? "此资源";
        if (MessageBox.Show($"确定删除资源「{name}」?",
            "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            _mcpService.DeleteResource(id);
            if (_editingId == id) { _editingId = 0; TxtResName.Clear(); TxtResPath.Clear(); }
            RefreshList();
        }
    }

    private async void BtnTestResource_Click(object sender, RoutedEventArgs e)
    {
        var id = GetIdFromSender(sender);
        if (id == 0) return;

        var btn = sender as Button;
        var orig = btn?.Content?.ToString();
        try
        {
            if (btn != null) { btn.IsEnabled = false; btn.Content = "..."; }

            var resource = _mcpService.GetAllResources().FirstOrDefault(r => r.Id == id);
            if (resource == null) { Log("资源不存在"); return; }

            Log($"=== 测试资源: {resource.Name} ===");
            Log($"类型: {resource.ResourceType}, 路径: {resource.Path}");

            var samples = _mcpService.SampleFiles(id, maxFiles: 5, sampleChars: 200, bypassCache: true);
            if (samples.Count == 0)
            {
                Log("未找到任何文件");
                return;
            }

            Log($"共发现文件样本: {samples.Count} 个");
            foreach (var s in samples)
            {
                var cached = s.IsCached ? "[缓存]" : "[新鲜]";
                Log($"  {cached} {s.FileName}");
                Log($"    预览: {s.Content[..Math.Min(s.Content.Length, 100)]}...");
            }
        }
        catch (Exception ex)
        {
            Log($"测试失败: {ex.Message}");
        }
        finally
        {
            if (btn != null) { btn.IsEnabled = true; btn.Content = orig ?? "测试"; }
        }
    }

    private void Log(string msg)
    {
        Dispatcher.Invoke(() =>
        {
            TbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            TbLog.ScrollToEnd();
        });
    }

    private void BtnBrowseResPath_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            TxtResPath.Text = dialog.SelectedPath;
    }

    private void BtnAddSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtResName.Text) || string.IsNullOrWhiteSpace(TxtResPath.Text))
        {
            MessageBox.Show("名称和路径不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var config = new McpResourceConfig
        {
            Name = TxtResName.Text.Trim(),
            ResourceType = (CmbResType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "LocalFolder",
            Path = TxtResPath.Text.Trim(),
            FileFilter = TxtResFilter.Text.Trim(),
            ExcludeFolders = TxtExcludeFolders.Text.Trim(),
        };

        if (_editingId > 0) config.Id = _editingId;

        _mcpService.SaveResource(config);
        _editingId = 0;
        TxtResName.Clear();
        TxtResPath.Clear();
        TxtResFilter.Text = "*.*";
        RefreshList();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}
