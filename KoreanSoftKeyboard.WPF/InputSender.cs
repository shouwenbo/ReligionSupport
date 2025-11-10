using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KoreanSoftKeyboard.WPF
{
    public static class InputSender
    {
        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public InputUnion U;
        }
        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
            [FieldOffset(0)] public MOUSEINPUT mi;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT { int dx; int dy; uint mouseData; uint dwFlags; uint time; UIntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Sequential)]
        struct HARDWAREINPUT { uint uMsg; ushort wParamL; ushort wParamH; }


        const uint INPUT_KEYBOARD = 1;
        const uint KEYEVENTF_UNICODE = 0x0004;
        const uint KEYEVENTF_KEYUP = 0x0002;


        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);


        public static void SendUnicodeString(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            var inputs = new INPUT[text.Length * 2];
            for (int i = 0; i < text.Length; i++)
            {
                inputs[i * 2] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion { ki = new KEYBDINPUT { wScan = text[i], dwFlags = KEYEVENTF_UNICODE } }
                };
                inputs[i * 2 + 1] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion { ki = new KEYBDINPUT { wScan = text[i], dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP } }
                };
            }
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
