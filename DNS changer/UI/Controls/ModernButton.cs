using System;
using System.Drawing;
using System.Windows.Forms;

namespace DNS_changer.UI.Controls
{
    internal enum ButtonKind
    {
        Primary,
        Secondary,
        Ghost,
        Danger,
    }

    internal sealed class ModernButton : CustomControl, IButtonControl
    {
        ButtonKind kind = ButtonKind.Secondary;
        Glyph glyph;
        bool hover, pressed;

        public ModernButton()
        {
            SetStyle(ControlStyles.Selectable | ControlStyles.StandardClick, true);
            Cursor = Cursors.Hand;
            Font = Theme.Font(9.5f, true);
            Size = Dpi.S(110, 38);
        }

        public ButtonKind Kind
        {
            get { return kind; }
            set { kind = value; Invalidate(); }
        }

        public Glyph Glyph
        {
            get { return glyph; }
            set { glyph = value; Invalidate(); }
        }

        public DialogResult DialogResult { get; set; }

        public void NotifyDefault(bool value)
        {
        }

        public void PerformClick()
        {
            if (Enabled && Visible)
                OnClick(EventArgs.Empty);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (DialogResult != DialogResult.None)
            {
                var form = FindForm();
                if (form != null)
                    form.DialogResult = DialogResult;
            }
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = pressed = false; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) { pressed = true; Invalidate(); }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            pressed = false;
            Invalidate();
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.Space)
                PerformClick();
        }

        protected override void Render(Graphics g)
        {
            var p = Theme.Current;
            var rect = new RectangleF(0, 0, Width, Height);
            float radius = Dpi.S(8);
            Color fill, text, border = Color.Empty;

            switch (kind)
            {
                case ButtonKind.Primary:
                    fill = pressed ? p.AccentPressed : hover ? p.AccentHover : p.Accent;
                    text = p.AccentText;
                    break;
                case ButtonKind.Ghost:
                    fill = pressed ? p.Border : hover ? p.SurfaceHover : ParentBack;
                    text = hover ? p.Text : p.TextMuted;
                    break;
                case ButtonKind.Danger:
                    fill = hover ? Gfx.Blend(p.Surface, p.Danger, pressed ? .25f : .14f) : p.Surface;
                    text = p.Danger;
                    border = hover ? Gfx.Blend(p.Border, p.Danger, .5f) : p.Border;
                    break;
                default:
                    fill = pressed ? p.Border : hover ? p.SurfaceHover : p.Surface;
                    text = p.Text;
                    border = p.Border;
                    break;
            }

            if (!Enabled)
            {
                fill = Gfx.Blend(fill, ParentBack, .45f);
                text = Gfx.Blend(text, ParentBack, .55f);
            }

            Gfx.FillRound(g, fill, rect, radius);
            if (border != Color.Empty)
                Gfx.DrawRound(g, border, rect, radius);
            if (Focused && ShowFocusCues)
                Gfx.DrawRound(g, p.Accent, rect, radius, Dpi.S(2));

            int iconSize = Dpi.S(16);
            bool hasText = !string.IsNullOrEmpty(Text);
            int textWidth = hasText ? Gfx.Measure(g, Text, Font) : 0;
            int gap = glyph != Glyph.None && hasText ? Dpi.S(8) : 0;
            int contentWidth = (glyph != Glyph.None ? iconSize : 0) + gap + textWidth;
            int x = Math.Max(Dpi.S(8), (Width - contentWidth) / 2);

            if (glyph != Glyph.None)
            {
                Glyphs.Draw(g, glyph, new RectangleF(x, (Height - iconSize) / 2f, iconSize, iconSize), text);
                x += iconSize + gap;
            }
            if (hasText)
                Gfx.Text(g, Text, Font, new Rectangle(x, 0, Width - x - Dpi.S(6), Height), text);
        }
    }
}
