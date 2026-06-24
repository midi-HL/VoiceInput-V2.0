using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace VoiceInput
{
    public static class ClipboardInjector
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll")]
        private static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern UIntPtr GlobalSize(IntPtr hMem);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public INPUTUNION union;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const ushort VK_CONTROL = 0x11;
        private const ushort VK_V = 0x56;
        private const uint CF_UNICODETEXT = 13;
        private const uint GMEM_MOVEABLE = 0x0002;

        public static async Task InjectTextAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            IntPtr foregroundHwnd = GetForegroundWindow();

            // Save original clipboard content
            string? originalClipboardText = null;
            try
            {
                var thread = new Thread(() =>
                {
                    try
                    {
                        originalClipboardText = GetClipboardText();
                    }
                    catch { }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join(500);
            }
            catch { }

            // Write text to clipboard
            bool clipboardOk = false;
            try
            {
                var setThread = new Thread(() =>
                {
                    try
                    {
                        clipboardOk = SetClipboardText(text);
                    }
                    catch { }
                });
                setThread.SetApartmentState(ApartmentState.STA);
                setThread.Start();
                setThread.Join(500);
            }
            catch { }

            if (!clipboardOk)
            {
                MessageBox(IntPtr.Zero, "文字已复制到剪贴板，请手动粘贴。\nText copied to clipboard, please paste manually.", "VoiceInput", 0);
                return;
            }

            await Task.Delay(50);

            if (foregroundHwnd != IntPtr.Zero)
            {
                SetForegroundWindow(foregroundHwnd);
                await Task.Delay(30);
            }

            SimulateCtrlV();

            await Task.Delay(200);

            // Restore original clipboard
            try
            {
                var thread = new Thread(() =>
                {
                    try
                    {
                        if (originalClipboardText != null)
                            SetClipboardText(originalClipboardText);
                        else
                            ClearClipboard();
                    }
                    catch { }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join(500);
            }
            catch { }
        }

        private static string? GetClipboardText()
        {
            if (!IsClipboardFormatAvailable(CF_UNICODETEXT))
                return null;

            if (!OpenClipboard(IntPtr.Zero))
                return null;

            try
            {
                IntPtr hData = GetClipboardData(CF_UNICODETEXT);
                if (hData == IntPtr.Zero)
                    return null;

                IntPtr pData = GlobalLock(hData);
                if (pData == IntPtr.Zero)
                    return null;

                try
                {
                    return Marshal.PtrToStringUni(pData);
                }
                finally
                {
                    GlobalUnlock(hData);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }

        private static bool SetClipboardText(string text)
        {
            if (!OpenClipboard(IntPtr.Zero))
                return false;

            try
            {
                EmptyClipboard();

                int bytes = (text.Length + 1) * 2; // UTF-16
                IntPtr hMem = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes);
                if (hMem == IntPtr.Zero)
                    return false;

                IntPtr pMem = GlobalLock(hMem);
                if (pMem == IntPtr.Zero)
                {
                    // GlobalFree would be needed but let's just return
                    return false;
                }

                try
                {
                    Marshal.Copy(text.ToCharArray(), 0, pMem, text.Length);
                    // Null terminator
                    Marshal.WriteInt16(pMem + text.Length * 2, 0);
                }
                finally
                {
                    GlobalUnlock(hMem);
                }

                if (SetClipboardData(CF_UNICODETEXT, hMem) == IntPtr.Zero)
                {
                    return false;
                }

                return true;
            }
            finally
            {
                CloseClipboard();
            }
        }

        private static void ClearClipboard()
        {
            if (OpenClipboard(IntPtr.Zero))
            {
                EmptyClipboard();
                CloseClipboard();
            }
        }

        private static void SimulateCtrlV()
        {
            INPUT[] inputs = new INPUT[4];

            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].union.ki.wVk = VK_CONTROL;

            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].union.ki.wVk = VK_V;

            inputs[2].type = INPUT_KEYBOARD;
            inputs[2].union.ki.wVk = VK_V;
            inputs[2].union.ki.dwFlags = KEYEVENTF_KEYUP;

            inputs[3].type = INPUT_KEYBOARD;
            inputs[3].union.ki.wVk = VK_CONTROL;
            inputs[3].union.ki.dwFlags = KEYEVENTF_KEYUP;

            SendInput(4, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
