using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KoreanSoftKeyboard.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KeyboardHook _hook;
        private Dictionary<int, KeyButton> _vkToButton = new Dictionary<int, KeyButton>();
        private bool _isShiftPressed = false;
        private readonly string workDir;

        public MainWindow()
        {
            InitializeComponent();

            AdaptSize();
            BuildKeyboard();
            StartHook();
            this.Deactivated += (s, e) => { /* 不取消高亮，保持响应 */ };


#if DEBUG
            workDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "work");
            Directory.CreateDirectory(workDir);
            OutputSourceFilesToTxt();
#endif
        }


        private void AdaptSize()
        {
            var screen = SystemParameters.WorkArea;
            this.Width = screen.Width * 0.5;
            this.Height = screen.Height * 0.35;
        }


        private void BtnAlwaysOnTop_Checked(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
        }


        private void BtnAlwaysOnTop_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
        }


        private void BtnMin_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();


        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _hook?.Dispose();
        }


        private void StartHook()
        {
            _hook = new KeyboardHook();
            _hook.KeyDown += Hook_KeyDown;
            _hook.KeyUp += Hook_KeyUp;
            _hook.Start();
        }


        private void Hook_KeyDown(object sender, KeyEventArgs e)
        {
            int vk = KeyInterop.VirtualKeyFromKey(e.Key);
            Dispatcher.Invoke(() => HighlightKey(vk, true));
        }


        private void Hook_KeyUp(object sender, KeyEventArgs e)
        {
            int vk = KeyInterop.VirtualKeyFromKey(e.Key);
            Dispatcher.Invoke(() => HighlightKey(vk, false));
        }


        private void HighlightKey(int vk, bool pressed)
        {
            if (_vkToButton.TryGetValue(vk, out var kb))
            {
                kb.IsPressed = pressed;
            }
        }

        // ====== 我实现的 BuildKeyboard：把 KeyboardLayout 的行渲染到 RowPanel ======
        private void BuildKeyboard()
        {
            // 清空旧内容（防止重复调用时重复添加）
            RowPanel.Items.Clear();
            _vkToButton.Clear();

            var rows = KeyboardLayout.DefaultLayout();
            foreach (var row in rows)
            {
                var sp = new System.Windows.Controls.StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    Margin = new Thickness(0, 6, 0, 0)
                };

                foreach (var k in row)
                {
                    var kb = new KeyButton(k);
                    kb.Margin = new Thickness(3, 0, 3, 0);
                    kb.Click += SoftKey_Click;
                    sp.Children.Add(kb);

                    // 防止左右Shift等使用相同VK覆盖，保留第一次映射
                    if (!_vkToButton.ContainsKey(k.VK))
                        _vkToButton[k.VK] = kb;
                }

                RowPanel.Items.Add(sp);
            }
        }

        // 软键被点击时发送字符到当前光标处，并短暂高亮键
        private async void SoftKey_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is KeyButton kb)
            {
                try
                {
                    // 发送到当前活动窗口的光标处（SendInput Unicode）
                    await System.Threading.Tasks.Task.Run(() => InputSender.SendUnicodeString(kb.OutputChar));
                }
                catch { }

                // 短暂高亮反馈
                kb.IsPressed = true;
                var t = new System.Windows.Threading.DispatcherTimer { Interval = System.TimeSpan.FromMilliseconds(120) };
                t.Tick += (s, ev) => { kb.IsPressed = false; t.Stop(); };
                t.Start();
            }
        }

        // 处理鼠标按下以便拖动窗口（Border 上的事件）
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    this.DragMove();
                }
            }
            catch { }
        }

        #region 输出源码到txt
        private void OutputSourceFilesToTxt()
        {
            try
            {
                string[] files =
                {
                    @"F:\code\ReligionSupport\KoreanSoftKeyboard.WPF\MainWindow.xaml",
                    @"F:\code\ReligionSupport\KoreanSoftKeyboard.WPF\MainWindow.xaml.cs",
                    @"F:\code\ReligionSupport\KoreanSoftKeyboard.WPF\InputSender.cs",
                    @"F:\code\ReligionSupport\KoreanSoftKeyboard.WPF\KeyboardLayout.cs",
                    @"F:\code\ReligionSupport\KoreanSoftKeyboard.WPF\KeyButton.cs",
                    @"F:\code\ReligionSupport\KoreanSoftKeyboard.WPF\KeyModel.cs",
                    @"F:\code\ReligionSupport\KoreanSoftKeyboard.WPF\RatioConverter.cs",
                    @"F:\code\ReligionSupport\KoreanSoftKeyboard.WPF\Win32.cs",
                };

                var sb = new StringBuilder();
                sb.AppendLine($"请严格以我提供的代码为基础，在其之上进行修改：");
                foreach (var file in files)
                {
                    if (File.Exists(file))
                    {
                        sb.AppendLine($"===== 文件: {file} =====");
                        sb.AppendLine(File.ReadAllText(file));
                        sb.AppendLine();
                    }
                    else
                        sb.AppendLine($"===== 文件未找到: {file} =====\n");
                }

                string outputPath = Path.Combine(workDir, "AllSourceOutput.txt");
                File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
                // Log($"源码已输出到 {outputPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"输出源码失败: {ex.Message}");
            }
        }
        #endregion
    }
}
