using System;
using System.Collections.Generic;
using System.Drawing;
using DNS_changer.Models;
using Microsoft.Win32;

namespace DNS_changer.UI
{
    internal sealed class Palette
    {
        public bool IsDark;
        public Color Background, Surface, SurfaceHover, Input, Border;
        public Color Text, TextMuted;
        public Color Accent, AccentHover, AccentPressed, AccentText;
        public Color Success, Warning, Danger, Scroll;
    }

    internal static class Theme
    {
        public static readonly Palette Dark = new Palette
        {
            IsDark = true,
            Background = Hex("#0F1115"),
            Surface = Hex("#181B22"),
            SurfaceHover = Hex("#20242D"),
            Input = Hex("#12151B"),
            Border = Hex("#282D38"),
            Text = Hex("#E9ECF2"),
            TextMuted = Hex("#8A93A6"),
            Accent = Hex("#4C8DFF"),
            AccentHover = Hex("#6A9FFF"),
            AccentPressed = Hex("#3A78E8"),
            AccentText = Color.White,
            Success = Hex("#3DD68C"),
            Warning = Hex("#F5B544"),
            Danger = Hex("#F26464"),
            Scroll = Hex("#353B49"),
        };

        public static readonly Palette Light = new Palette
        {
            IsDark = false,
            Background = Hex("#F3F5F9"),
            Surface = Color.White,
            SurfaceHover = Hex("#EDF1F8"),
            Input = Color.White,
            Border = Hex("#DCE1EA"),
            Text = Hex("#171B24"),
            TextMuted = Hex("#667085"),
            Accent = Hex("#2F6BFF"),
            AccentHover = Hex("#4A7FFF"),
            AccentPressed = Hex("#2459DB"),
            AccentText = Color.White,
            Success = Hex("#12A363"),
            Warning = Hex("#C77E00"),
            Danger = Hex("#D93B3B"),
            Scroll = Hex("#C9D0DC"),
        };

        static readonly Color[] AvatarColors =
        {
            Hex("#4C8DFF"), Hex("#8B5CF6"), Hex("#EC4899"), Hex("#F59E0B"),
            Hex("#10B981"), Hex("#06B6D4"), Hex("#F97316"), Hex("#6366F1"),
        };

        static readonly Dictionary<string, Font> Fonts = new Dictionary<string, Font>();

        public static Palette Current { get; private set; } = Dark;

        public static event EventHandler Changed;

        public static void Apply(ThemeMode mode)
        {
            bool dark = mode == ThemeMode.Dark || (mode == ThemeMode.System && SystemPrefersDark());
            var next = dark ? Dark : Light;
            if (next == Current)
                return;
            Current = next;
            Changed?.Invoke(null, EventArgs.Empty);
        }

        public static Font Font(float size, bool semibold = false)
        {
            string key = size + (semibold ? "b" : "");
            Font font;
            if (Fonts.TryGetValue(key, out font))
                return font;

            if (semibold)
            {
                font = new Font("Segoe UI Semibold", size);
                if (font.FontFamily.Name != "Segoe UI Semibold")
                {
                    font.Dispose();
                    font = new Font("Segoe UI", size, FontStyle.Bold);
                }
            }
            else
            {
                font = new Font("Segoe UI", size);
            }
            Fonts[key] = font;
            return font;
        }

        public static Color AvatarColor(string key)
        {
            int hash = 17;
            foreach (char c in key ?? "")
                hash = unchecked(hash * 31 + c);
            return AvatarColors[(hash & 0x7FFFFFFF) % AvatarColors.Length];
        }

        static bool SystemPrefersDark()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    object value = key?.GetValue("AppsUseLightTheme");
                    // Windows 7/8 have no app theme setting: use the dark look by default.
                    return value == null || Convert.ToInt32(value) == 0;
                }
            }
            catch
            {
                return true;
            }
        }

        static Color Hex(string hex) => ColorTranslator.FromHtml(hex);
    }
}
