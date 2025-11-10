using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace KoreanSoftKeyboard.WPF
{
    // 简化的全局键盘钩子
    public class KeyboardHook : IDisposable
    {
        private IntPtr _hookId = IntPtr.Zero;
        private Win32.LowLevelKeyboardProc _proc;


        public event EventHandler<KeyEventArgs> KeyDown;
        public event EventHandler<KeyEventArgs> KeyUp;


        public KeyboardHook()
        {
            _proc = HookCallback;
        }


        public void Start()
        {
            _hookId = Win32.SetHook(_proc);
        }


        public void Dispose()
        {
            if (_hookId != IntPtr.Zero) { Win32.UnhookWindowsHookEx(_hookId); _hookId = IntPtr.Zero; }
        }


        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            const int WM_KEYDOWN = 0x0100;
            const int WM_KEYUP = 0x0101;
            const int WM_SYSKEYDOWN = 0x0104;
            const int WM_SYSKEYUP = 0x0105;


            if (nCode >= 0)
            {
                int vk = Marshal.ReadInt32(lParam);
                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    KeyDown?.Invoke(this, new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(System.Windows.Application.Current.MainWindow), 0, KeyInterop.KeyFromVirtualKey(vk)));
                }
                else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                {
                    KeyUp?.Invoke(this, new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(System.Windows.Application.Current.MainWindow), 0, KeyInterop.KeyFromVirtualKey(vk)));
                }
            }
            return Win32.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }
    }
}
