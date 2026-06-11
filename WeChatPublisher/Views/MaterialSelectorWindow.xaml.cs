using System.Windows;
using System.Windows.Controls;
using WeChatPublisher.Services;

namespace WeChatPublisher.Views;

public partial class MaterialSelectorWindow : Window
{
    private readonly McpService _mcp = new();
    public List<SelectableItem> SelectedMaterials { get; private set; } = [];
    public List<SelectableItem> SelectedVerses { get; private set; } = [];

    public MaterialSelectorWindow()
    {
        InitializeComponent();
        AppIcon.Set(this);
        Loaded += async (_, _) =>
        {
            await RefreshMaterialsAsync();
            var biblePath = AppSettings.Instance.BibleDbPath;
            if (File.Exists(biblePath)) await RefreshVersesAsync(biblePath);
        };
    }

    private async Task RefreshMaterialsAsync()
    {
        BtnRefreshMaterials.IsEnabled = false;
        try
        {
            var items = await MaterialLoader.LoadMaterialsAsync(_mcp);
            LbMaterials.ItemsSource = items;
            TbMaterialCount.Text = $"{items.Count}篇";
        }
        catch { }
        finally { BtnRefreshMaterials.IsEnabled = true; }
    }

    private async Task RefreshVersesAsync(string biblePath)
    {
        BtnRefreshVerses.IsEnabled = false;
        try
        {
            var items = await MaterialLoader.LoadVersesAsync(biblePath);
            LbVerses.ItemsSource = items;
            TbVerseCount.Text = $"{items.Count}条";
        }
        catch { }
        finally { BtnRefreshVerses.IsEnabled = true; }
    }

    private void BtnRefreshMaterials_Click(object s, RoutedEventArgs e) => _ = RefreshMaterialsAsync();
    private void BtnRefreshVerses_Click(object s, RoutedEventArgs e)
    {
        var biblePath = AppSettings.Instance.BibleDbPath;
        if (File.Exists(biblePath)) _ = RefreshVersesAsync(biblePath);
    }

    private void LbMaterials_MouseDoubleClick(object s, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (LbMaterials.SelectedItem is SelectableItem item)
            new DetailWindow(item.SourceLabel, item.SourceDetail, item.Data).Show();
    }
    private void LbVerses_MouseDoubleClick(object s, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (LbVerses.SelectedItem is SelectableItem item)
            new DetailWindow("随机经文", "来自: 圣经数据库", item.Data).Show();
    }

    private void BtnConfirm_Click(object sender, RoutedEventArgs e)
    {
        if (LbMaterials.ItemsSource is List<SelectableItem> mats)
            SelectedMaterials = mats.Where(m => m.IsSelected).ToList();
        if (LbVerses.ItemsSource is List<SelectableItem> verses)
            SelectedVerses = verses.Where(v => v.IsSelected).ToList();
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();
}
