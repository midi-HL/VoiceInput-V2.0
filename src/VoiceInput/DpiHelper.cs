using System;
using System.Runtime.InteropServices;

namespace VoiceInput
{
    public static class DpiHelper
    {
        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        private enum MonitorDpiType
        {
            MDT_EFFECTIVE_DPI = 0,
            MDT_ANGULAR_DPI = 1,
            MDT_RAW_DPI = 2
        }

        private const uint MONITOR_DEFAULTTONEAREST = 2;

        public static double GetDpiScaleForPoint(int x, int y)
        {
            POINT pt = new POINT { X = x, Y = y };
            IntPtr monitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
            return GetDpiScaleForMonitor(monitor);
        }

        public static double GetDpiScaleForWindow(IntPtr hwnd)
        {
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            return GetDpiScaleForMonitor(monitor);
        }

        private static double GetDpiScaleForMonitor(IntPtr monitor)
        {
            try
            {
                int hr = GetDpiForMonitor(monitor, MonitorDpiType.MDT_EFFECTIVE_DPI, out uint dpiX, out _);
                if (hr == 0 && dpiX > 0)
                {
                    return dpiX / 96.0;
                }
            }
            catch { }
            return 1.0;
        }

        public static (int left, int top, int width, int height) GetWorkingAreaForPoint(int x, int y)
        {
            POINT pt = new POINT { X = x, Y = y };
            IntPtr monitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
            return GetMonitorWorkingArea(monitor);
        }

        private static (int left, int top, int width, int height) GetMonitorWorkingArea(IntPtr monitor)
        {
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (GetMonitorInfo(monitor, ref mi))
            {
                return (mi.rcWork.Left, mi.rcWork.Top, mi.rcWork.Right - mi.rcWork.Left, mi.rcWork.Bottom - mi.rcWork.Top);
            }
            return (0, 0, 1920, 1080); // Fallback
        }
    }
}
