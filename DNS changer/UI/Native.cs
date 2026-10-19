using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace DNS_changer.UI
{
    internal static class Native
    {
        const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;
        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        const int DWMWA_CAPTION_COLOR = 35;
        const int DWMWA_TEXT_COLOR = 36;
        public const int EM_SETCUEBANNER = 0x1501;

        /// <summary>Colors the native title bar on Windows 10/11; silently ignored on older systems.</summary>
        public static void StyleTitleBar(IntPtr handle, bool dark, Color caption, Color text)
        {
            try
            {
                int value = dark ? 1 : 0;
                if (DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, 4) != 0)
                    DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref value, 4);

                int captionRef = ToColorRef(caption);
                DwmSetWindowAttribute(handle, DWMWA_CAPTION_COLOR, ref captionRef, 4);
                int textRef = ToColorRef(text);
                DwmSetWindowAttribute(handle, DWMWA_TEXT_COLOR, ref textRef, 4);
            }
            catch
            {
                // dwmapi missing or attribute unsupported.
            }
        }

        public static void BringToFront(IntPtr handle)
        {
            try
            {
                ShowWindow(handle, 9 /* SW_RESTORE */);
                SetForegroundWindow(handle);
            }
            catch
            {
            }
        }

        static int ToColorRef(Color c) => c.R | (c.G << 8) | (c.B << 16);

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);
    }
}
