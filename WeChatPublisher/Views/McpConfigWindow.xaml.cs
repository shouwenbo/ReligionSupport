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
