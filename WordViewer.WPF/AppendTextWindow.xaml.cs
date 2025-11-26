using System.Windows;

namespace WordViewer.WPF
{
    public partial class AppendTextWindow : Window
    {
        public string InputText { get; private set; } = string.Empty;

        public AppendTextWindow()
        {
            InitializeComponent();
            Loaded += (_, __) => InputTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            InputText = InputTextBox.Text ?? string.Empty;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
