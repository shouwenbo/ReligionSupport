using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace WordViewer.WPF
{
    public partial class MainWindow : Window
    {
        private FlowDocument _document;
        private readonly string workDir;
        private string _currentFilePath = string.Empty;

        // Visual Studio 2022 明亮 & 暗黑主题
        private enum ThemeMode { Light, Dark }
        private ThemeMode _currentTheme = ThemeMode.Light;

        // 明亮主题（VS 2022 默认浅色）
        private readonly SolidColorBrush LightBackground = new SolidColorBrush(Color.FromRgb(250, 250, 250));
        private readonly SolidColorBrush LightForeground = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        private readonly SolidColorBrush LightToolbar = new SolidColorBrush(Color.FromRgb(240, 240, 240));

        // 暗黑主题（VS 2022 深色）
        private readonly SolidColorBrush DarkBackground = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        private readonly SolidColorBrush DarkForeground = new SolidColorBrush(Color.FromRgb(241, 241, 241));
        private readonly SolidColorBrush DarkToolbar = new SolidColorBrush(Color.FromRgb(45, 45, 48));

        public MainWindow()
        {
            InitializeComponent();
            workDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "work");
            EnsureWorkDir();

            _document = new FlowDocument
            {
                TextAlignment = TextAlignment.Left,
                FontFamily = new FontFamily("微软雅黑"),
                FontSize = 14
            };

            DocumentViewer.Document = _document;
            ApplyTheme();
            OutputSourceFilesToTxt();

            // 文本随窗口宽度自适应
            DocumentViewer.SizeChanged += (s, e) =>
            {
                _document.PageWidth = e.NewSize.Width - 40;
            };

            // 🚫 禁用 Ctrl + 滚轮缩放行为（关键）
            DocumentViewer.PreviewMouseWheel += (s, e) =>
            {
                if ((Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) ||
                     Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl)))
                {
                    e.Handled = true; // 阻止默认缩放行为
                }
            };
        }

        private void EnsureWorkDir()
        {
            if (Directory.Exists(workDir))
            {
                DirectoryInfo di = new DirectoryInfo(workDir);
                foreach (var file in di.GetFiles()) file.Delete();
                foreach (var dir in di.GetDirectories()) dir.Delete(true);
            }
            else Directory.CreateDirectory(workDir);
        }

        #region 打开文件（支持拖拽）
        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Word 文档 (*.docx)|*.docx",
                InitialDirectory = workDir
            };
            if (dlg.ShowDialog() == true)
                LoadWordFile(dlg.FileName);
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1 && Path.GetExtension(files[0]).Equals(".docx", StringComparison.OrdinalIgnoreCase))
                    e.Effects = DragDropEffects.Copy;
            }
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    string file = files[0];
                    if (Path.GetExtension(file).Equals(".docx", StringComparison.OrdinalIgnoreCase))
                        LoadWordFile(file);
                    else
                        MessageBox.Show("仅支持 .docx 格式的文件。", "格式不支持", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void LoadWordFile(string filePath)
        {
            try
            {
                using var doc = DocX.Load(filePath);
                string[] paras = new string[doc.Paragraphs.Count];
                for (int i = 0; i < doc.Paragraphs.Count; i++)
                    paras[i] = doc.Paragraphs[i].Text;
                DisplayParagraphs(paras);
                _currentFilePath = filePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"读取文件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayParagraphs(string[] paragraphs)
        {
            _document.Blocks.Clear();
            foreach (var para in paragraphs)
            {
                if (!string.IsNullOrWhiteSpace(para))
                {
                    System.Windows.Documents.Paragraph p = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(para))
                    {
                        TextAlignment = TextAlignment.Left,
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    _document.Blocks.Add(p);
                }
            }
        }
        #endregion

        #region 主题切换
        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            _currentTheme = _currentTheme == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            if (_currentTheme == ThemeMode.Light)
            {
                _document.Background = LightBackground;
                _document.Foreground = LightForeground;
                MainToolBar.Background = LightToolbar;
                MainToolBar.Foreground = LightForeground;
                Background = LightBackground;
            }
            else
            {
                _document.Background = DarkBackground;
                _document.Foreground = DarkForeground;
                MainToolBar.Background = DarkToolbar;
                MainToolBar.Foreground = DarkForeground;
                Background = DarkBackground;
            }
        }
        #endregion

        #region 字体大小调节
        private void IncreaseFont_Click(object sender, RoutedEventArgs e)
        {
            _document.FontSize = Math.Min(_document.FontSize + 1, 72);
        }

        private void DecreaseFont_Click(object sender, RoutedEventArgs e)
        {
            _document.FontSize = Math.Max(_document.FontSize - 1, 8);
        }

        private void ChangeFont_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FontDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var f = dlg.Font;
                _document.FontFamily = new FontFamily(f.Name);
                _document.FontSize = f.Size * 96.0 / 72.0;
                _document.FontWeight = f.Bold ? FontWeights.Bold : FontWeights.Regular;
                _document.FontStyle = f.Italic ? FontStyles.Italic : FontStyles.Normal;
            }
        }
        #endregion

        #region 打开Work目录
        private void OpenWorkFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(workDir))
                System.Diagnostics.Process.Start("explorer.exe", workDir);
        }
        #endregion

        #region 输出源码
        private void OutputSourceFilesToTxt()
        {
            try
            {
                string[] files =
                {
                    @"F:\code\ReligionSupport\WordViewer.WPF\MainWindow.xaml",
                    @"F:\code\ReligionSupport\WordViewer.WPF\MainWindow.xaml.cs",
                };
                var sb = new StringBuilder();
                sb.AppendLine("请严格以我提供的代码为基础，在其之上进行修改：");
                foreach (var file in files)
                {
                    if (File.Exists(file))
                    {
                        sb.AppendLine($"===== 文件: {file} =====");
                        sb.AppendLine(File.ReadAllText(file));
                        sb.AppendLine();
                    }
                    else sb.AppendLine($"===== 文件未找到: {file} =====\n");
                }
                File.WriteAllText(Path.Combine(workDir, "AllSourceOutput.txt"), sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"输出源码失败: {ex.Message}");
            }
        }
        #endregion

        #region 追加文本
        private void AppendTextButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentFilePath) || !File.Exists(_currentFilePath))
            {
                MessageBox.Show("请先打开一个 Word 文档。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new AppendTextWindow { Owner = this };
            if (dialog.ShowDialog() != true) return;

            var payload = dialog.InputText?.Replace("\r\n", "\n").TrimEnd();
            if (string.IsNullOrWhiteSpace(payload))
            {
                MessageBox.Show("请输入要追加的内容。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                AppendTextToWord(_currentFilePath!, payload);
                MessageBox.Show("文本追加成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                Process.Start(new ProcessStartInfo { FileName = _currentFilePath, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"追加文本失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AppendTextToWord(string filePath, string inputText)
        {
            var today = DateTime.Now;
            var headerLine = $"{today.Year - 1983}{today:MMdd}整理：";

            using (var document = DocX.Load(filePath))
            {
                document.InsertParagraph(); // 空一行
                InsertFormattedParagraph(document, headerLine);

                foreach (var line in inputText.Split('\n'))
                {
                    InsertFormattedParagraph(document, line);
                }

                document.Save();
            }
        }

        private static void InsertFormattedParagraph(DocX document, string content)
        {
            var paragraph = document.InsertParagraph(content ?? string.Empty);
            ApplyParagraphFormatting(paragraph);
        }

        private static void ApplyParagraphFormatting(Xceed.Document.NET.Paragraph paragraph)
        {
            paragraph.Font("KaiTi").FontSize(11);
            paragraph.SpacingBefore(0).SpacingAfter(0);
            paragraph.SetLineSpacing(LineSpacingType.Line, 12);
            DisableSnapToGrid(paragraph);
        }

        private static void DisableSnapToGrid(Xceed.Document.NET.Paragraph paragraph)
        {
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            var xml = paragraph.Xml;
            var pPr = xml.Element(w + "pPr");
            if (pPr == null)
            {
                pPr = new XElement(w + "pPr");
                xml.AddFirst(pPr);
            }

            var snap = pPr.Element(w + "snapToGrid");
            if (snap == null)
            {
                snap = new XElement(w + "snapToGrid");
                pPr.Add(snap);
            }

            snap.SetAttributeValue(w + "val", "0");
        }
        #endregion
    }
}
