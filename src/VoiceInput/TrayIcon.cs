using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace VoiceInput
{
    public class TrayIcon : IDisposable
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll")]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

        [DllImport("user32.dll")]
        private static extern bool TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

        [DllImport("user32.dll")]
        private static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public uint dwState;
            public uint dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public uint uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public uint dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        private const uint NIM_ADD = 0x00000000;
        private const uint NIM_MODIFY = 0x00000001;
        private const uint NIM_DELETE = 0x00000002;
        private const uint NIF_MESSAGE = 0x00000001;
        private const uint NIF_ICON = 0x00000002;
        private const uint NIF_TIP = 0x00000004;
        private const uint NIF_INFO = 0x00000010;
        private const uint WM_USER = 0x0400;
        private const uint WM_TRAYICON = WM_USER + 1;
        private const uint WM_RBUTTONUP = 0x0205;
        private const uint WM_LBUTTONUP = 0x0202;
        private const uint WM_LBUTTONDBLCLK = 0x0203;
        private const uint WM_COMMAND = 0x0111;
        private const uint MF_STRING = 0x00000000;
        private const uint MF_SEPARATOR = 0x00000800;
        private const uint TPM_RIGHTBUTTON = 0x0002;
        private const uint TPM_BOTTOMALIGN = 0x0020;
        private const uint GWL_WNDPROC = -4;

        private const uint MENU_ID_ENABLE = 1001;
        private const uint MENU_ID_HOME = 1002;
        private const uint MENU_ID_LANG_ZH = 1010;
        private const uint MENU_ID_LANG_EN = 1011;
        private const uint MENU_ID_LANG_ZH_TW = 1012;
        private const uint MENU_ID_LANG_JA = 1013;
        private const uint MENU_ID_LANG_KO = 1014;
        private const uint MENU_ID_MODE_LOCAL = 1020;
        private const uint MENU_ID_MODE_AI = 1021;
        private const uint MENU_ID_SETTINGS = 1030;
        private const uint MENU_ID_ABOUT = 1040;
        private const uint MENU_ID_EXIT = 1050;

        private NOTIFYICONDATA _notifyData;
        private IntPtr _hWnd;
        private IntPtr _hIcon;
        private bool _disposed;
        private bool _enabled = true;

        public event Action? ShowHomeRequested;
        public event Action? ShowSettingsRequested;
        public event Action? ExitRequested;
        public event Action<bool>? EnableChanged;

        public void Initialize()
        {
            _hIcon = CreateTrayIcon();

            // Create a hidden window to receive messages
            _hWnd = CreateMessageWindow();

            _notifyData = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hWnd,
                uID = 1,
                uFlags = NIF_ICON | NIF_TIP | NIF_MESSAGE,
                uCallbackMessage = WM_TRAYICON,
                hIcon = _hIcon,
                szTip = "VoiceInput - 语音输入"
            };

            Shell_NotifyIcon(NIM_ADD, ref _notifyData);
        }

        private IntPtr CreateMessageWindow()
        {
            string className = "VoiceInputTrayClass";
            IntPtr hInstance = GetModuleHandle(null);

            var wc = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = WndProc,
                hInstance = hInstance,
                lpszClassName = className
            };

            RegisterClassEx(ref wc);

            return CreateWindowEx(0, className, "VoiceInputTray", 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);
        }

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private static WndProcDelegate? _wndProcDelegate;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASSEX
        {
            public int cbSize;
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_TRAYICON)
            {
                uint mouseMsg = (uint)lParam.ToInt32();
                if (mouseMsg == WM_RBUTTONUP)
                {
                    ShowContextMenu();
                }
                else if (mouseMsg == WM_LBUTTONDBLCLK)
                {
                    _instance?.ShowHomeRequested?.Invoke();
                }
                return IntPtr.Zero;
            }
            if (msg == WM_COMMAND)
            {
                uint menuId = (uint)wParam.ToInt32() & 0xFFFF;
                _instance?.HandleMenuCommand(menuId);
                return IntPtr.Zero;
            }
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private static TrayIcon? _instance;

        public void InitializeWithInstance()
        {
            _instance = this;
            _wndProcDelegate = WndProc;
            Initialize();
        }

        private static void ShowContextMenu()
        {
            GetCursorPos(out POINT pt);
            IntPtr hMenu = CreatePopupMenu();

            // Enable/Disable
            AppendMenu(hMenu, MF_STRING, MENU_ID_ENABLE, _instance!._enabled ? "语音输入  ✓ 已启用" : "语音输入  ✗ 已禁用");
            AppendMenu(hMenu, MF_SEPARATOR, 0, "");
            AppendMenu(hMenu, MF_STRING, MENU_ID_HOME, "主页");
            AppendMenu(hMenu, MF_SEPARATOR, 0, "");

            // Language submenu
            IntPtr langMenu = CreatePopupMenu();
            AppendMenu(langMenu, MF_STRING, MENU_ID_LANG_ZH, Settings.RecognitionLanguage == RecognitionLanguage.ZhCN ? "● 简体中文" : "  简体中文");
            AppendMenu(langMenu, MF_STRING, MENU_ID_LANG_EN, Settings.RecognitionLanguage == RecognitionLanguage.EnUS ? "● English" : "  English");
            AppendMenu(langMenu, MF_STRING, MENU_ID_LANG_ZH_TW, Settings.RecognitionLanguage == RecognitionLanguage.ZhTW ? "● 繁體中文" : "  繁體中文");
            AppendMenu(langMenu, MF_STRING, MENU_ID_LANG_JA, Settings.RecognitionLanguage == RecognitionLanguage.JaJP ? "● 日本語" : "  日本語");
            AppendMenu(langMenu, MF_STRING, MENU_ID_LANG_KO, Settings.RecognitionLanguage == RecognitionLanguage.KoKR ? "● 한국어" : "  한국어");
            AppendMenu(hMenu, 0x00000010, 0, "识别语言"); // MF_POPUP
            AppendMenu(hMenu, MF_SEPARATOR, 0, "");

            // Mode submenu
            IntPtr modeMenu = CreatePopupMenu();
            AppendMenu(modeMenu, MF_STRING, MENU_ID_MODE_LOCAL, Settings.RecognitionMode == RecognitionMode.LocalWithLlmCorrection ? "● 本地识别 + LLM 纠错" : "  本地识别 + LLM 纠错");
            AppendMenu(modeMenu, MF_STRING, MENU_ID_MODE_AI, Settings.RecognitionMode == RecognitionMode.AiTranscription ? "● AI 语音转写（API）" : "  AI 语音转写（API）");
            AppendMenu(hMenu, 0x00000010, 0, "识别模式"); // MF_POPUP
            AppendMenu(hMenu, MF_SEPARATOR, 0, "");
            AppendMenu(hMenu, MF_STRING, MENU_ID_SETTINGS, "设置");
            AppendMenu(hMenu, MF_SEPARATOR, 0, "");
            AppendMenu(hMenu, MF_STRING, MENU_ID_ABOUT, "关于...");
            AppendMenu(hMenu, MF_STRING, MENU_ID_EXIT, "退出");

            TrackPopupMenu(hMenu, TPM_RIGHTBUTTON | TPM_BOTTOMALIGN, pt.X, pt.Y, 0, _instance!._hWnd, IntPtr.Zero);
            DestroyMenu(hMenu);
            DestroyMenu(langMenu);
            DestroyMenu(modeMenu);
        }

        public void HandleMenuCommand(uint menuId)
        {
            switch (menuId)
            {
                case MENU_ID_ENABLE:
                    _enabled = !_enabled;
                    EnableChanged?.Invoke(_enabled);
                    break;
                case MENU_ID_HOME:
                    ShowHomeRequested?.Invoke();
                    break;
                case MENU_ID_LANG_ZH:
                    Settings.RecognitionLanguage = RecognitionLanguage.ZhCN;
                    break;
                case MENU_ID_LANG_EN:
                    Settings.RecognitionLanguage = RecognitionLanguage.EnUS;
                    break;
                case MENU_ID_LANG_ZH_TW:
                    Settings.RecognitionLanguage = RecognitionLanguage.ZhTW;
                    break;
                case MENU_ID_LANG_JA:
                    Settings.RecognitionLanguage = RecognitionLanguage.JaJP;
                    break;
                case MENU_ID_LANG_KO:
                    Settings.RecognitionLanguage = RecognitionLanguage.KoKR;
                    break;
                case MENU_ID_MODE_LOCAL:
                    Settings.RecognitionMode = RecognitionMode.LocalWithLlmCorrection;
                    break;
                case MENU_ID_MODE_AI:
                    Settings.RecognitionMode = RecognitionMode.AiTranscription;
                    break;
                case MENU_ID_SETTINGS:
                    ShowSettingsRequested?.Invoke();
                    break;
                case MENU_ID_ABOUT:
                    ShowAbout();
                    break;
                case MENU_ID_EXIT:
                    ExitRequested?.Invoke();
                    break;
            }
        }

        private void ShowAbout()
        {
            // Simple message box via Win32
            MessageBox(IntPtr.Zero, "VoiceInput v1.0.0\n\nWindows 系统托盘语音输入法\n\n按住右 Alt 键开始语音输入", "关于 VoiceInput", 0);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        private static IntPtr CreateTrayIcon()
        {
            int size = 256;
            using var bmp = new Bitmap(size, size);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.Transparent);

            using var brush = new SolidBrush(Color.White);
            using var pen = new Pen(Color.White, size * 0.04f);

            float cx = size / 2f;
            float cy = size * 0.35f;
            float micWidth = size * 0.2f;
            float micHeight = size * 0.3f;

            var micRect = new RectangleF(cx - micWidth / 2, cy - micHeight / 2, micWidth, micHeight);
            using var micPath = new System.Drawing.Drawing2D.GraphicsPath();
            float radius = micWidth * 0.4f;
            micPath.AddArc(micRect.X, micRect.Y, radius * 2, radius * 2, 180, 90);
            micPath.AddArc(micRect.Right - radius * 2, micRect.Y, radius * 2, radius * 2, 270, 90);
            micPath.AddLine(micRect.Right, micRect.Bottom, micRect.X, micRect.Bottom);
            micPath.CloseFigure();
            g.FillPath(brush, micPath);

            float arcRadius = size * 0.18f;
            g.DrawArc(pen, cx - arcRadius, cy + micHeight * 0.3f, arcRadius * 2, arcRadius * 2, 0, 180);

            float standX = cx;
            float standTop = cy + micHeight * 0.3f + arcRadius;
            float standBottom = standTop + size * 0.1f;
            g.DrawLine(pen, standX, standTop, standX, standBottom);

            float baseWidth = size * 0.15f;
            g.DrawLine(pen, standX - baseWidth, standBottom, standX + baseWidth, standBottom);

            IntPtr hIcon = bmp.GetHicon();
            return hIcon;
        }

        public void ShowBalloonTip(string title, string text)
        {
            _notifyData.szInfoTitle = title;
            _notifyData.szInfo = text;
            _notifyData.uFlags = NIF_INFO;
            Shell_NotifyIcon(NIM_MODIFY, ref _notifyData);
            _notifyData.uFlags = NIF_ICON | NIF_TIP | NIF_MESSAGE;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Shell_NotifyIcon(NIM_DELETE, ref _notifyData);
                if (_hIcon != IntPtr.Zero)
                {
                    DestroyIcon(_hIcon);
                    _hIcon = IntPtr.Zero;
                }
                if (_hWnd != IntPtr.Zero)
                {
                    DestroyWindow(_hWnd);
                    _hWnd = IntPtr.Zero;
                }
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        ~TrayIcon()
        {
            Dispose();
        }
    }
}
