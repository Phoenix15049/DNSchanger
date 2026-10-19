using System;
using System.Drawing;
using System.Windows.Forms;

namespace DNS_changer.UI.Controls
{
    internal interface IThemeable
    {
        void ApplyTheme();
    }

    /// <summary>Form that follows <see cref="Theme"/> and re-styles its children when it changes.</summary>
    internal class ThemedForm : Form
    {
        static Icon appIcon;

        public ThemedForm()
        {
            AutoScaleMode = AutoScaleMode.None;
            Font = Theme.Font(9f);
            DoubleBuffered = true;
            Icon = AppIcon;
            Theme.Changed += OnThemeChanged;
        }

        static Icon AppIcon
        {
            get
            {
                if (appIcon == null)
                {
                    try { appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
                    catch { }
                }
                return appIcon;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyTheme();
        }

        protected virtual void ApplyTheme()
        {
            var p = Theme.Current;
            BackColor = p.Background;
            ForeColor = p.Text;
            if (IsHandleCreated)
                Native.StyleTitleBar(Handle, p.IsDark, p.Background, p.Text);
            Restyle(Controls);
            Invalidate(true);
        }

        static void Restyle(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                (control as IThemeable)?.ApplyTheme();
                Restyle(control.Controls);
            }
        }

        void OnThemeChanged(object sender, EventArgs e)
        {
            if (IsHandleCreated && !IsDisposed)
                ApplyTheme();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Theme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }

    internal enum LabelStyle
    {
        Normal,
        Muted,
        Title,
        Header,
        Success,
        Warning,
        Error,
    }

    internal sealed class ThemedLabel : Label, IThemeable
    {
        LabelStyle style;

        public ThemedLabel(LabelStyle style = LabelStyle.Normal)
        {
            BackColor = Color.Transparent;
            UseMnemonic = false;
            Style = style;
        }

        public LabelStyle Style
        {
            get { return style; }
            set
            {
                style = value;
                switch (style)
                {
                    case LabelStyle.Title: Font = Theme.Font(15f, true); break;
                    case LabelStyle.Header: Font = Theme.Font(10.5f, true); break;
                    case LabelStyle.Muted: Font = Theme.Font(9f); break;
                    default: Font = Theme.Font(9.5f); break;
                }
                ApplyTheme();
            }
        }

        public void ApplyTheme()
        {
            var p = Theme.Current;
            switch (style)
            {
                case LabelStyle.Muted: ForeColor = p.TextMuted; break;
                case LabelStyle.Success: ForeColor = p.Success; break;
                case LabelStyle.Warning: ForeColor = p.Warning; break;
                case LabelStyle.Error: ForeColor = p.Danger; break;
                default: ForeColor = p.Text; break;
            }
        }
    }

    internal sealed class ThemedLink : LinkLabel, IThemeable
    {
        public ThemedLink()
        {
            BackColor = Color.Transparent;
            LinkBehavior = LinkBehavior.HoverUnderline;
            Font = Theme.Font(9f);
            AutoSize = true;
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            LinkColor = Theme.Current.Accent;
            ActiveLinkColor = Theme.Current.AccentPressed;
            VisitedLinkColor = Theme.Current.Accent;
        }
    }

    /// <summary>Base for owner-drawn controls: double buffered, paints its own background.</summary>
    internal abstract class CustomControl : Control
    {
        protected CustomControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        protected Color ParentBack => Parent?.BackColor ?? Theme.Current.Background;

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(ParentBack);
            Gfx.Smooth(e.Graphics);
            Render(e.Graphics);
        }

        protected abstract void Render(Graphics g);

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }
    }
}
