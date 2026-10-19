using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DNS_changer.UI.Controls
{
    /// <summary>Pill-shaped tab strip with equally sized segments.</summary>
    internal sealed class SegmentedControl : CustomControl
    {
        readonly List<string> items = new List<string>();
        int selected, hover = -1;

        public SegmentedControl()
        {
            SetStyle(ControlStyles.Selectable, true);
            Font = Theme.Font(9.5f, true);
            Height = Dpi.S(38);
            Cursor = Cursors.Hand;
        }

        public event EventHandler SelectedIndexChanged;

        public int Count => items.Count;

        public string SelectedItem => selected >= 0 && selected < items.Count ? items[selected] : null;

        public int SelectedIndex
        {
            get { return selected; }
            set
            {
                value = Math.Max(0, Math.Min(items.Count - 1, value));
                if (value == selected)
                    return;
                selected = value;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SetItems(IEnumerable<string> values, string select = null)
        {
            items.Clear();
            items.AddRange(values);
            int index = select == null ? -1 : items.IndexOf(select);
            selected = index >= 0 ? index : 0;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        int IndexAt(Point point)
        {
            if (items.Count == 0 || !ClientRectangle.Contains(point))
                return -1;
            return Math.Min(items.Count - 1, point.X * items.Count / Math.Max(1, Width));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = IndexAt(e.Location);
            if (index != hover) { hover = index; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = -1; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int index = IndexAt(e.Location);
            if (index >= 0)
                SelectedIndex = index;
        }

        protected override bool IsInputKey(Keys keyData) =>
            keyData == Keys.Left || keyData == Keys.Right || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Left) SelectedIndex--;
            if (e.KeyCode == Keys.Right) SelectedIndex++;
        }

        protected override void Render(Graphics g)
        {
            var p = Theme.Current;
            float radius = Dpi.S(10);
            Gfx.FillRound(g, p.Surface, new RectangleF(0, 0, Width, Height), radius);
            Gfx.DrawRound(g, Focused && ShowFocusCues ? p.Accent : p.Border, new RectangleF(0, 0, Width, Height), radius);
            if (items.Count == 0)
                return;

            int inset = Dpi.S(4);
            float segment = (Width - inset * 2f) / items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                var rect = new RectangleF(inset + i * segment, inset, segment, Height - inset * 2);
                Color text = p.TextMuted;
                if (i == selected)
                {
                    Gfx.FillRound(g, p.Accent, rect, radius - inset / 2f);
                    text = p.AccentText;
                }
                else if (i == hover)
                {
                    Gfx.FillRound(g, p.SurfaceHover, rect, radius - inset / 2f);
                    text = p.Text;
                }
                Gfx.Text(g, items[i], Font, Rectangle.Round(rect), text, TextFormatFlags.HorizontalCenter);
            }
        }
    }

    internal sealed class ToggleSwitch : CustomControl
    {
        bool isChecked, hover;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.Selectable | ControlStyles.StandardClick, true);
            Font = Theme.Font(9.5f);
            Height = Dpi.S(30);
            Cursor = Cursors.Hand;
        }

        public event EventHandler CheckedChanged;

        public bool Checked
        {
            get { return isChecked; }
            set
            {
                if (isChecked == value)
                    return;
                isChecked = value;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Checked = !Checked;
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.Space)
                Checked = !Checked;
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }

        protected override void Render(Graphics g)
        {
            var p = Theme.Current;
            int w = Dpi.S(40), h = Dpi.S(22);
            var track = new RectangleF(0, (Height - h) / 2f, w, h);
            Color fill = isChecked ? (hover ? p.AccentHover : p.Accent) : (hover ? Gfx.Blend(p.Border, p.TextMuted, .3f) : p.Border);
            Gfx.FillRound(g, fill, track, h / 2f);
            if (Focused && ShowFocusCues)
                Gfx.DrawRound(g, p.Accent, RectangleF.Inflate(track, Dpi.S(2), Dpi.S(2)), h / 2f + Dpi.S(2), Dpi.S(2));

            float knob = h - Dpi.S(6);
            float x = isChecked ? track.Right - knob - Dpi.S(3) : track.X + Dpi.S(3);
            using (var brush = new SolidBrush(isChecked ? Color.White : Gfx.Blend(p.Surface, Color.White, .6f)))
                g.FillEllipse(brush, x, track.Y + Dpi.S(3), knob, knob);

            int textX = w + Dpi.S(12);
            Gfx.Text(g, Text, Font, new Rectangle(textX, 0, Width - textX, Height), p.Text);
        }
    }

    /// <summary>Borderless TextBox inside a rounded, themed frame.</summary>
    internal sealed class ModernTextBox : CustomControl, IThemeable
    {
        readonly TextBox box = new TextBox { BorderStyle = BorderStyle.None };
        string placeholder;
        bool hasError;

        public ModernTextBox()
        {
            box.Font = Theme.Font(10f);
            box.GotFocus += (s, e) => Invalidate();
            box.LostFocus += (s, e) => Invalidate();
            box.TextChanged += (s, e) => OnTextChanged(e);
            box.HandleCreated += (s, e) => ApplyPlaceholder();
            Controls.Add(box);
            Height = Dpi.S(40);
            Cursor = Cursors.IBeam;
            ApplyTheme();
        }

        public override string Text
        {
            get { return box.Text; }
            set { box.Text = value; }
        }

        public string Placeholder
        {
            get { return placeholder; }
            set { placeholder = value; ApplyPlaceholder(); }
        }

        public bool HasError
        {
            get { return hasError; }
            set { hasError = value; Invalidate(); }
        }

        public void FocusInput()
        {
            box.Focus();
            box.SelectAll();
        }

        public void ApplyTheme()
        {
            box.BackColor = Theme.Current.Input;
            box.ForeColor = Theme.Current.Text;
            Invalidate();
        }

        void ApplyPlaceholder()
        {
            if (box.IsHandleCreated && placeholder != null)
                Native.SendMessage(box.Handle, Native.EM_SETCUEBANNER, (IntPtr)1, placeholder);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            box.Focus();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            box.Focus();
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            int pad = Dpi.S(12);
            box.SetBounds(pad, (Height - box.PreferredHeight) / 2, Width - pad * 2, box.PreferredHeight);
        }

        protected override void Render(Graphics g)
        {
            var p = Theme.Current;
            var rect = new RectangleF(0, 0, Width, Height);
            Gfx.FillRound(g, p.Input, rect, Dpi.S(8));
            Color border = hasError ? p.Danger : box.Focused ? p.Accent : p.Border;
            Gfx.DrawRound(g, border, rect, Dpi.S(8), box.Focused || hasError ? Dpi.S(2) : 1f);
        }
    }

    /// <summary>Combo-box replacement that opens a themed menu.</summary>
    internal sealed class DropDownSelector : CustomControl
    {
        readonly List<string> items = new List<string>();
        int selected = -1;
        bool hover;
        ContextMenuStrip menu;

        public DropDownSelector()
        {
            SetStyle(ControlStyles.Selectable, true);
            Font = Theme.Font(9.5f);
            Height = Dpi.S(40);
            Cursor = Cursors.Hand;
        }

        public event EventHandler SelectedIndexChanged;

        public List<string> Items => items;

        public int SelectedIndex
        {
            get { return selected; }
            set
            {
                if (value == selected)
                    return;
                selected = value;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (e.Button == MouseButtons.Left)
                ShowMenu();
        }

        protected override bool IsInputKey(Keys keyData) =>
            keyData == Keys.Down || keyData == Keys.Up || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.F4 || (e.Alt && e.KeyCode == Keys.Down))
                ShowMenu();
            else if (e.KeyCode == Keys.Down && selected < items.Count - 1)
                SelectedIndex++;
            else if (e.KeyCode == Keys.Up && selected > 0)
                SelectedIndex--;
        }

        void ShowMenu()
        {
            if (items.Count == 0)
                return;
            menu?.Dispose();
            menu = new ContextMenuStrip
            {
                Renderer = new ThemedMenuRenderer(),
                ShowImageMargin = false,
                ShowCheckMargin = true,
                Font = Font,
                MinimumSize = new Size(Width, 0),
            };
            for (int i = 0; i < items.Count; i++)
            {
                int index = i;
                var item = new ToolStripMenuItem(items[i]) { Checked = i == selected, Padding = new Padding(0, Dpi.S(5), 0, Dpi.S(5)) };
                item.Click += (s, e) => SelectedIndex = index;
                menu.Items.Add(item);
            }
            menu.Show(this, new Point(0, Height + Dpi.S(4)));
        }

        protected override void Render(Graphics g)
        {
            var p = Theme.Current;
            var rect = new RectangleF(0, 0, Width, Height);
            Gfx.FillRound(g, hover ? p.SurfaceHover : p.Input, rect, Dpi.S(8));
            Gfx.DrawRound(g, Focused && ShowFocusCues ? p.Accent : p.Border, rect, Dpi.S(8), Focused && ShowFocusCues ? Dpi.S(2) : 1f);

            int icon = Dpi.S(14), pad = Dpi.S(12);
            string text = selected >= 0 && selected < items.Count ? items[selected] : "";
            Gfx.Text(g, text, Font, new Rectangle(pad, 0, Width - pad * 3 - icon, Height), p.Text);
            Glyphs.Draw(g, Glyph.ChevronDown, new RectangleF(Width - pad - icon, (Height - icon) / 2f, icon, icon), p.TextMuted);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                menu?.Dispose();
            base.Dispose(disposing);
        }
    }

    internal sealed class ThemedMenuRenderer : ToolStripProfessionalRenderer
    {
        public ThemedMenuRenderer() : base(new ThemedColorTable())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Theme.Current.Text;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            int size = Dpi.S(14);
            var r = e.ImageRectangle;
            Glyphs.Draw(e.Graphics, Glyph.Check,
                new RectangleF(r.X + (r.Width - size) / 2f, r.Y + (r.Height - size) / 2f, size, size), Theme.Current.Accent);
        }

        sealed class ThemedColorTable : ProfessionalColorTable
        {
            static Palette P => Theme.Current;
            public override Color MenuBorder => P.Border;
            public override Color MenuItemBorder => P.SurfaceHover;
            public override Color MenuItemSelected => P.SurfaceHover;
            public override Color MenuItemSelectedGradientBegin => P.SurfaceHover;
            public override Color MenuItemSelectedGradientEnd => P.SurfaceHover;
            public override Color MenuItemPressedGradientBegin => P.SurfaceHover;
            public override Color MenuItemPressedGradientEnd => P.SurfaceHover;
            public override Color ToolStripDropDownBackground => P.Surface;
            public override Color ImageMarginGradientBegin => P.Surface;
            public override Color ImageMarginGradientMiddle => P.Surface;
            public override Color ImageMarginGradientEnd => P.Surface;
            public override Color CheckBackground => Color.Transparent;
            public override Color CheckSelectedBackground => Color.Transparent;
            public override Color CheckPressedBackground => Color.Transparent;
            public override Color SeparatorDark => P.Border;
            public override Color SeparatorLight => P.Border;
        }
    }
}
