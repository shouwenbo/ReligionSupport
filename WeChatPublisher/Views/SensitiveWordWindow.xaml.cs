using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using WeChatPublisher.Models;
using WeChatPublisher.Services;

namespace WeChatPublisher.Views;

public partial class SensitiveWordWindow : Window
{
    private readonly SensitiveWordService _service = new();
    private int _editingId;

    public static IValueConverter StrictLevelConverter { get; } = new StrictLevelValueConverter();

    public SensitiveWordWindow()
    {
        InitializeComponent();
        AppIcon.Set(this);
        Loaded += (_, _) =>
        {
            _service.SeedDefaults();
            RefreshList();
        };
    }

    private void RefreshList()
    {
        _service.RefreshCache();
        DgWords.ItemsSource = _service.GetAll();
    }

    private void DgWords_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgWords.SelectedItem is not SensitiveWord word) return;
        _editingId = word.Id;
        TxtSourceWord.Text = word.SourceWord;
        TxtReplacements.Text = word.ReplacementWords;
        SelectCategory(word.Category);
        SelectStrictLevel(word.StrictLevel);
    }

    private void SelectCategory(string cat)
    {
        foreach (ComboBoxItem item in CmbCategory.Items)
            if (item.Content?.ToString() == cat) { CmbCategory.SelectedItem = item; return; }
    }

    private void SelectStrictLevel(int level)
    {
        foreach (ComboBoxItem item in CmbStrictLevel.Items)
            if (item.Tag?.ToString() == level.ToString()) { CmbStrictLevel.SelectedItem = item; return; }
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        _editingId = 0;
        TxtSourceWord.Clear();
        TxtReplacements.Clear();
        CmbCategory.SelectedIndex = 0;
        CmbStrictLevel.SelectedIndex = 1;
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (DgWords.SelectedItem is not SensitiveWord word) return;
        if (MessageBox.Show($"确定删除规则 '{word.SourceWord}'?", "确认",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _service.Delete(word.Id);
            RefreshList();
        }
    }

    private void BtnImport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "CSV文件|*.csv|所有文件|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var lines = File.ReadAllLines(dialog.FileName);
                int imported = 0;
                foreach (var line in lines.Skip(1))
                {
                    var parts = line.Split(',');
                    if (parts.Length < 4) continue;
                    _service.Save(new SensitiveWord
                    {
                        SourceWord = parts[0].Trim(),
                        ReplacementWords = parts[1].Trim(),
                        Category = parts[2].Trim(),
                        StrictLevel = int.TryParse(parts[3], out var sl) ? sl : 0,
                        Notes = parts.Length > 4 ? parts[4].Trim() : null
                    });
                    imported++;
                }
                RefreshList();
                MessageBox.Show($"导入成功: {imported} 条", "导入结果",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV文件|*.csv",
            FileName = "sensitive_words.csv"
        };
        if (dialog.ShowDialog() == true)
        {
            var words = _service.GetAll();
            var lines = new List<string> { "源词,替换词,类别,严格度,备注" };
            lines.AddRange(words.Select(w =>
                $"\"{w.SourceWord}\",\"{w.ReplacementWords}\",\"{w.Category}\",{w.StrictLevel},\"{w.Notes ?? ""}\""));
            File.WriteAllLines(dialog.FileName, lines);
            MessageBox.Show("导出成功", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshList();
    }

    private void BtnSaveRule_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtSourceWord.Text))
        {
            MessageBox.Show("源词不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var word = new SensitiveWord
        {
            SourceWord = TxtSourceWord.Text.Trim(),
            ReplacementWords = TxtReplacements.Text.Trim(),
            Category = (CmbCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "自定义",
            StrictLevel = int.Parse(((CmbStrictLevel.SelectedItem as ComboBoxItem)?.Tag as string) ?? "1"),
            IsEnabled = 1
        };

        if (_editingId > 0) word.Id = _editingId;

        _service.Save(word);
        _editingId = 0;
        TxtSourceWord.Clear();
        TxtReplacements.Clear();
        RefreshList();
    }

    private void BtnTest_Click(object sender, RoutedEventArgs e)
    {
        var text = TxtTestInput.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            LblTestResult.Content = "请先输入测试文本";
            return;
        }

        var matches = _service.Scan(text);
        if (matches.Count == 0)
        {
            LblTestResult.Content = "未检测到敏感词";
            LblTestResult.Foreground = new SolidColorBrush(Colors.Green);
        }
        else
        {
            var forced = matches.Where(m => m.Rule.StrictLevel == 1).ToList();
            var suggested = matches.Where(m => m.Rule.StrictLevel == 0).ToList();

            var result = $"检测到 {matches.Count} 处敏感词";
            if (forced.Count > 0)
                result += $" (强制: {forced.Count} [{string.Join(", ", forced.Select(m => m.MatchedText))}])";
            if (suggested.Count > 0)
                result += $" (建议: {suggested.Count} [{string.Join(", ", suggested.Select(m => m.MatchedText))}])";

            LblTestResult.Content = result;
            LblTestResult.Foreground = forced.Count > 0
                ? new SolidColorBrush(Colors.Red)
                : new SolidColorBrush(Colors.Orange);
        }
    }
}

public class StrictLevelValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter,
        System.Globalization.CultureInfo culture)
    {
        return value is int level && level >= 1
            ? new SolidColorBrush(Colors.Red)
            : new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter,
        System.Globalization.CultureInfo culture) => throw new NotImplementedException();
}
