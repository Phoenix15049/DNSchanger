using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DNS_changer.UI
{
    /// <summary>DPI scaling: the app is system-DPI aware and lays itself out in code.</summary>
    internal static class Dpi
    {
        public static float Factor { get; private set; } = 1f;

        public static void Init()
        {
            try
            {
                using (var g = Graphics.FromHwnd(IntPtr.Zero))
                    Factor = Math.Max(1f, g.DpiX / 96f);
            }
            catch
            {
                Factor = 1f;
            }
        }

        public static int S(int value) => (int)Math.Round(value * Factor);
        public static Size S(int width, int height) => new Size(S(width), S(height));
    }

    internal static class Gfx
    {
        public static void Smooth(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }

        public static GraphicsPath Round(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            if (d <= 0.5f)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void FillRound(Graphics g, Color color, RectangleF r, float radius)
        {
            using (var brush = new SolidBrush(color))
            using (var path = Round(r, radius))
                g.FillPath(brush, path);
        }

        public static void DrawRound(Graphics g, Color color, RectangleF r, float radius, float width = 1f)
        {
            float inset = width / 2f;
            var rect = new RectangleF(r.X + inset, r.Y + inset, r.Width - width, r.Height - width);
            using (var pen = new Pen(color, width))
            using (var path = Round(rect, radius))
                g.DrawPath(pen, path);
        }

        public static Color Blend(Color from, Color to, float amount)
        {
            amount = Math.Max(0, Math.Min(1, amount));
            return Color.FromArgb(
                (int)(from.R + (to.R - from.R) * amount),
                (int)(from.G + (to.G - from.G) * amount),
                (int)(from.B + (to.B - from.B) * amount));
        }

        public const TextFormatFlags SingleLine =
            TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;

        public static void Text(Graphics g, string text, Font font, Rectangle bounds, Color color,
            TextFormatFlags extra = TextFormatFlags.Left)
        {
            TextRenderer.DrawText(g, text, font, bounds, color, SingleLine | extra);
        }

        public static int Measure(Graphics g, string text, Font font)
        {
            return TextRenderer.MeasureText(g, text, font, new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
        }
    }
}
