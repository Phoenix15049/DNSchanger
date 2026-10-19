using System.Drawing;
using System.Windows.Forms;

namespace DNS_changer.UI.Controls
{
    internal enum StatusKind
    {
        Loading,
        Preset,
        Custom,
        Automatic,
        Offline,
    }

    /// <summary>Card showing the DNS currently in use on the active adapter.</summary>
    internal sealed class StatusCard : CustomControl
    {
        StatusKind kind = StatusKind.Loading;
        string title = "Checking…", detail = "", adapter = "";

        public StatusCard()
        {
            Height = Dpi.S(100);
        }

        public void Set(StatusKind kind, string title, string detail, string adapter)
        {
            this.kind = kind;
            this.title = title ?? "";
            this.detail = detail ?? "";
            this.adapter = adapter ?? "";
            Invalidate();
        }

        protected override void Render(Graphics g)
        {
            var p = Theme.Current;
            var rect = new RectangleF(0, 0, Width, Height);
            float radius = Dpi.S(14);

            Color tone;
            Glyph glyph;
            switch (kind)
            {
                case StatusKind.Preset: tone = p.Success; glyph = Glyph.Shield; break;
                case StatusKind.Custom: tone = p.Accent; glyph = Glyph.Shield; break;
                case StatusKind.Offline: tone = p.Danger; glyph = Glyph.Offline; break;
                case StatusKind.Automatic: tone = p.Warning; glyph = Glyph.Globe; break;
                default: tone = p.TextMuted; glyph = Glyph.Globe; break;
            }

            Gfx.FillRound(g, p.Surface, rect, radius);
            Gfx.DrawRound(g, p.Border, rect, radius);

            // Accent strip on the left edge.
            var state = g.Save();
            using (var clip = Gfx.Round(rect, radius))
            {
                g.SetClip(clip);
                using (var brush = new SolidBrush(tone))
                    g.FillRectangle(brush, 0, 0, Dpi.S(4), Height);
            }
            g.Restore(state);

            int pad = Dpi.S(20);
            int badge = Dpi.S(46);
            var badgeRect = new RectangleF(pad, (Height - badge) / 2f, badge, badge);
            Gfx.FillRound(g, Gfx.Blend(p.Surface, tone, p.IsDark ? .18f : .12f), badgeRect, Dpi.S(12));
            int icon = Dpi.S(22);
            Glyphs.Draw(g, glyph, new RectangleF(badgeRect.X + (badge - icon) / 2f, badgeRect.Y + (badge - icon) / 2f, icon, icon), tone);

            int x = pad + badge + Dpi.S(16);
            int right = Width - pad;
            int top = (Height - Dpi.S(66)) / 2;

            var captionFont = Theme.Font(8f, true);
            Gfx.Text(g, "CURRENT DNS", captionFont, new Rectangle(x, top, Dpi.S(120), Dpi.S(16)), p.TextMuted);

            if (adapter.Length > 0)
            {
                var adapterFont = Theme.Font(8.5f);
                int maxWidth = (right - x) / 2;
                int width = System.Math.Min(maxWidth, Gfx.Measure(g, adapter, adapterFont));
                int dot = Dpi.S(7);
                int adapterX = right - width;
                using (var brush = new SolidBrush(kind == StatusKind.Offline ? p.Danger : p.Success))
                    g.FillEllipse(brush, adapterX - dot - Dpi.S(6), top + (Dpi.S(16) - dot) / 2f, dot, dot);
                Gfx.Text(g, adapter, adapterFont, new Rectangle(adapterX, top, width, Dpi.S(16)), p.TextMuted);
            }

            Gfx.Text(g, title, Theme.Font(14f, true), new Rectangle(x, top + Dpi.S(17), right - x, Dpi.S(30)), p.Text);
            Gfx.Text(g, detail, Theme.Font(9f), new Rectangle(x, top + Dpi.S(48), right - x, Dpi.S(18)), p.TextMuted);
        }
    }
}
