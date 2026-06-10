using System.Windows;

namespace WeChatPublisher.Views;

public partial class DetailWindow : Window
{
    public DetailWindow(string title, string source, string content)
    {
        InitializeComponent();
        AppIcon.Set(this);
        TbDetailSource.Text = title;
        TbDetailMeta.Text = source;
        TbDetailContent.Text = content;
    }

    private void BtnCopy_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(TbDetailContent.Text);
        MessageBox.Show("已复制到剪贴板", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}
