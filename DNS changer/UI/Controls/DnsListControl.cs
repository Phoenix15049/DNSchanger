using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DNS_changer.Models;

namespace DNS_changer.UI.Controls
{
    internal sealed class DnsEntryEventArgs : EventArgs
    {
        public DnsEntryEventArgs(DnsEntry entry) { Entry = entry; }
        public DnsEntry Entry { get; }
    }

    /// <summary>
    /// Scrollable list of DNS cards. In normal mode a click activates an entry;
    /// in <see cref="SelectionMode"/> a click selects and double-click activates.
    /// </summary>
    internal sealed class DnsListControl : CustomControl
    {
        const int Testing = int.MinValue;

        readonly Dictionary<string, int> latency = new Dictionary<string, int>();
        List<DnsEntry> items = new List<DnsEntry>();
        int selected = -1, hover = -1, pressedIndex = -1;
        int scroll;
        bool draggingThumb, thumbHover;
        int dragStartY, dragStartScroll;
        DnsEntry active, busy;

        public DnsListControl()
        {
            SetStyle(ControlStyles.Selectable, true);
        }

        public event EventHandler<DnsEntryEventArgs> EntryActivated;
        public event EventHandler SelectedIndexChanged;

        public bool SelectionMode { get; set; }
        public string EmptyText { get; set; } = "No DNS servers in this list.";

        public DnsEntry ActiveEntry
        {
            get { return active; }
            set { active = value; Invalidate(); }
        }

        public DnsEntry BusyEntry
        {
            get { return busy; }
            set { busy = value; Invalidate(); }
        }

        public int SelectedIndex
        {
            get { return selected; }
            set
            {
                value = items.Count == 0 ? -1 : Math.Max(0, Math.Min(items.Count - 1, value));
                if (value == selected)
                    return;
                selected = value;
                EnsureVisible(selected);
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public DnsEntry SelectedEntry => selected >= 0 && selected < items.Count ? items[selected] : null;

        public void SetItems(List<DnsEntry> entries)
        {
            items = entries ?? new List<DnsEntry>();
            if (selected >= items.Count)
                selected = items.Count - 1;
            if (!SelectionMode)
                selected = -1;
            hover = -1;
            ClampScroll();
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetTesting(string server) { latency[server ?? ""] = Testing; Invalidate(); }

        public void SetLatency(string server, int milliseconds) { latency[server ?? ""] = milliseconds; Invalidate(); }

        // ---- geometry ----

        int RowHeight => Dpi.S(64);
        int Gap => Dpi.S(8);
        int ContentHeight => items.Count == 0 ? 0 : items.Count * (RowHeight + Gap) - Gap;
        int MaxScroll => Math.Max(0, ContentHeight - Height);
        bool ScrollbarVisible => MaxScroll > 0;
        int CardWidth => Width - (ScrollbarVisible ? Dpi.S(14) : 0);

        Rectangle RowRect(int index) =>
            new Rectangle(0, index * (RowHeight + Gap) - scroll, CardWidth, RowHeight);

        Rectangle ThumbRect
        {
            get
            {
                if (!ScrollbarVisible)
                    return Rectangle.Empty;
                int track = Height;
                int thumb = Math.Max(Dpi.S(36), track * Height / ContentHeight);
                int y = (int)((track - thumb) * (scroll / (float)MaxScroll));
                return new Rectangle(Width - Dpi.S(7), y, Dpi.S(6), thumb);
            }
        }

        int IndexAt(Point point)
        {
            if (point.X < 0 || point.X >= CardWidth)
                return -1;
            int y = point.Y + scroll;
            if (y < 0)
                return -1;
            int index = y / (RowHeight + Gap);
            if (index >= items.Count || y % (RowHeight + Gap) >= RowHeight)
                return -1;
            return index;
        }

        void ClampScroll()
        {
            scroll = Math.Max(0, Math.Min(MaxScroll, scroll));
        }

        void EnsureVisible(int index)
        {
            if (index < 0)
                return;
            int top = index * (RowHeight + Gap);
            if (top < scroll)
                scroll = top;
            else if (top + RowHeight > scroll + Height)
                scroll = top + RowHeight - Height;
            ClampScroll();
        }

        void UpdateHover(Point point)
        {
            int index = draggingThumb ? -1 : IndexAt(point);
            bool overThumb = ThumbRect.Contains(point) || draggingThumb;
            if (index != hover || overThumb != thumbHover)
            {
                hover = index;
                thumbHover = overThumb;
                Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        // ---- input ----

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ClampScroll();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (draggingThumb)
            {
                int track = Height - ThumbRect.Height;
                if (track > 0)
                {
                    scroll = dragStartScroll + (int)((e.Y - dragStartY) * (MaxScroll / (float)track));
                    ClampScroll();
                    Invalidate();
                }
            }
            UpdateHover(e.Location);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hover = -1;
            thumbHover = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (e.Button != MouseButtons.Left)
                return;

            if (ThumbRect.Contains(e.Location))
            {
                draggingThumb = true;
                dragStartY = e.Y;
                dragStartScroll = scroll;
                return;
            }

            pressedIndex = IndexAt(e.Location);
            if (SelectionMode && pressedIndex >= 0)
                SelectedIndex = pressedIndex;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (draggingThumb)
            {
                draggingThumb = false;
                UpdateHover(e.Location);
                return;
            }
            int index = IndexAt(e.Location);
            if (!SelectionMode && e.Button == MouseButtons.Left && index >= 0 && index == pressedIndex)
                EntryActivated?.Invoke(this, new DnsEntryEventArgs(items[index]));
            pressedIndex = -1;
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            int index = IndexAt(e.Location);
            if (SelectionMode && index >= 0)
                EntryActivated?.Invoke(this, new DnsEntryEventArgs(items[index]));
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            scroll -= e.Delta * (RowHeight + Gap) / 120;
            ClampScroll();
            UpdateHover(e.Location);
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData) =>
            keyData == Keys.Up || keyData == Keys.Down || keyData == Keys.Enter ||
            keyData == Keys.Home || keyData == Keys.End || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.Down: SelectedIndex = selected + 1; break;
                case Keys.Up: SelectedIndex = selected < 0 ? 0 : selected - 1; break;
                case Keys.Home: SelectedIndex = 0; break;
                case Keys.End: SelectedIndex = items.Count - 1; break;
                case Keys.Enter:
                    if (SelectedEntry != null)
                        EntryActivated?.Invoke(this, new DnsEntryEventArgs(SelectedEntry));
                    break;
            }
        }

        // ---- painting ----

        protected override void Render(Graphics g)
        {
            var p = Theme.Current;

            if (items.Count == 0)
            {
                Gfx.Text(g, EmptyText, Theme.Font(9.5f), ClientRectangle, p.TextMuted, TextFormatFlags.HorizontalCenter);
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                var rect = RowRect(i);
                if (rect.Bottom < 0 || rect.Top > Height)
                    continue;
                DrawRow(g, p, items[i], i, rect);
            }

            if (ScrollbarVisible)
            {
                var thumb = ThumbRect;
                Gfx.FillRound(g, thumbHover ? p.TextMuted : p.Scroll, thumb, thumb.Width / 2f);
            }
        }

        void DrawRow(Graphics g, Palette p, DnsEntry entry, int index, Rectangle rect)
        {
            bool isActive = entry == active;
            bool isSelected = SelectionMode && index == selected;
            bool isHover = index == hover;
            bool keyboardFocus = !SelectionMode && index == selected && Focused && ShowFocusCues;

            Color back = p.Surface;
            if (isActive)
                back = Gfx.Blend(p.Surface, p.Accent, p.IsDark ? .14f : .07f);
            else if (isHover || isSelected)
                back = p.SurfaceHover;

            Color border = isActive || isSelected || keyboardFocus ? p.Accent : p.Border;
            float radius = Dpi.S(12);
            Gfx.FillRound(g, back, rect, radius);
            Gfx.DrawRound(g, border, rect, radius, isSelected || keyboardFocus ? Dpi.S(2) : 1f);

            int pad = Dpi.S(14);
            int avatar = Dpi.S(38);
            var avatarRect = new RectangleF(rect.X + pad, rect.Y + (rect.Height - avatar) / 2f, avatar, avatar);
            var tone = Theme.AvatarColor(entry.Name);
            Gfx.FillRound(g, Gfx.Blend(back, tone, p.IsDark ? .22f : .14f), avatarRect, Dpi.S(10));
            string initial = string.IsNullOrEmpty(entry.Name) ? "?" : entry.Name.Substring(0, 1).ToUpperInvariant();
            Gfx.Text(g, initial, Theme.Font(11f, true), Rectangle.Round(avatarRect), tone, TextFormatFlags.HorizontalCenter);

            // Right-side badges, laid out from the right edge.
            int right = rect.Right - pad;
            var badgeFont = Theme.Font(8.5f, true);
            int badgeHeight = Dpi.S(24);
            int badgeY = rect.Y + (rect.Height - badgeHeight) / 2;

            if (entry == busy)
            {
                right = DrawBadge(g, "Applying…", null, badgeFont, right, badgeY, badgeHeight, back, p.Accent, false);
            }
            else if (SelectionMode)
            {
                right = DrawBadge(g, entry.Category ?? "", null, badgeFont, right, badgeY, badgeHeight, back, p.TextMuted, false);
            }
            else
            {
                int ms;
                if (latency.TryGetValue(entry.Primary ?? "", out ms))
                {
                    if (ms == Testing)
                        right = DrawBadge(g, "• • •", null, badgeFont, right, badgeY, badgeHeight, back, p.TextMuted, false);
                    else if (ms < 0)
                        right = DrawBadge(g, "Timeout", null, badgeFont, right, badgeY, badgeHeight, back, p.Danger, false);
                    else
                        right = DrawBadge(g, ms + " ms", null, badgeFont, right, badgeY, badgeHeight, back, LatencyColor(p, ms), false);
                }
                if (isActive)
                    right = DrawBadge(g, "Active", Glyph.Check, badgeFont, right, badgeY, badgeHeight, back, p.Accent, true);
            }

            int textX = (int)avatarRect.Right + Dpi.S(12);
            int textWidth = Math.Max(0, right - textX - Dpi.S(4));
            int middle = rect.Y + rect.Height / 2;
            Gfx.Text(g, entry.Name, Theme.Font(10f, true), new Rectangle(textX, middle - Dpi.S(20), textWidth, Dpi.S(21)), p.Text);
            Gfx.Text(g, entry.AddressText, Theme.Font(8.5f), new Rectangle(textX, middle + Dpi.S(1), textWidth, Dpi.S(18)), p.TextMuted);
        }

        static Color LatencyColor(Palette p, int ms)
        {
            if (ms <= 80) return p.Success;
            if (ms <= 200) return p.Warning;
            return p.Danger;
        }

        /// <returns>The new right edge for the next badge.</returns>
        static int DrawBadge(Graphics g, string text, Glyph? glyph, Font font, int right, int y, int height,
            Color back, Color tone, bool solid)
        {
            if (string.IsNullOrEmpty(text))
                return right;
            int pad = Dpi.S(9);
            int icon = glyph.HasValue ? Dpi.S(12) : 0;
            int gap = glyph.HasValue ? Dpi.S(4) : 0;
            int width = pad * 2 + icon + gap + Gfx.Measure(g, text, font);
            var rect = new Rectangle(right - width, y, width, height);

            Color fill = solid ? tone : Gfx.Blend(back, tone, .16f);
            Color fore = solid ? Color.White : tone;
            Gfx.FillRound(g, fill, rect, height / 2f);

            int x = rect.X + pad;
            if (glyph.HasValue)
            {
                Glyphs.Draw(g, glyph.Value, new RectangleF(x, y + (height - icon) / 2f, icon, icon), fore);
                x += icon + gap;
            }
            Gfx.Text(g, text, font, new Rectangle(x, y, rect.Right - x, height), fore);
            return rect.X - Dpi.S(6);
        }
    }
}
