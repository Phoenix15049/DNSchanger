using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DNS_changer.Core;
using DNS_changer.Models;
using DNS_changer.UI.Controls;

namespace DNS_changer.UI
{
    internal sealed class SettingsForm : ThemedForm
    {
        const string RepositoryUrl = "https://github.com/Phoenix15049/DNSchanger";

        readonly List<DnsEntry> servers;
        readonly ThemeMode originalTheme;
        readonly List<string> adapterIds = new List<string>();

        readonly DnsListControl list = new DnsListControl { SelectionMode = true, EmptyText = "The list is empty. Add a server or restore the defaults." };
        readonly ModernButton btnAdd = new ModernButton { Kind = ButtonKind.Primary, Glyph = Glyph.Plus, Text = "Add" };
        readonly ModernButton btnEdit = new ModernButton { Kind = ButtonKind.Secondary, Glyph = Glyph.Pencil, Text = "Edit" };
        readonly ModernButton btnDelete = new ModernButton { Kind = ButtonKind.Danger, Glyph = Glyph.Trash, Text = "Delete" };
        readonly ModernButton btnUp = new ModernButton { Kind = ButtonKind.Secondary, Glyph = Glyph.ChevronUp };
        readonly ModernButton btnDown = new ModernButton { Kind = ButtonKind.Secondary, Glyph = Glyph.ChevronDown };
        readonly ModernButton btnDefaults = new ModernButton { Kind = ButtonKind.Ghost, Glyph = Glyph.Reset, Text = "Restore defaults" };
        readonly DropDownSelector adapterSelector = new DropDownSelector();
        readonly ToggleSwitch tglFlush = new ToggleSwitch { Text = "Flush DNS cache after applying" };
        readonly ToggleSwitch tglTest = new ToggleSwitch { Text = "Test speed when the app starts" };
        readonly SegmentedControl themeSelector = new SegmentedControl();
        readonly ToolTip tips = new ToolTip();
        int separatorY;

        public SettingsForm(AppSettings settings, IList<NetworkAdapter> adapters)
        {
            servers = settings.Servers.Select(s => s.Clone()).ToList();
            originalTheme = settings.Theme;

            Text = "Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            int pad = Dpi.S(24), width = Dpi.S(760);
            int leftWidth = Dpi.S(400);
            int rightX = pad + leftWidth + Dpi.S(32);
            int rightWidth = width - rightX - pad;
            int top = Dpi.S(20);

            // ---- left column: DNS servers ----
            AddHeader("DNS servers", "Double-click a server to edit it.", pad, top);
            btnDefaults.SetBounds(pad + leftWidth - Dpi.S(150), top - Dpi.S(4), Dpi.S(150), Dpi.S(34));
            list.SetBounds(pad, top + Dpi.S(56), leftWidth, Dpi.S(318));
            int row = list.Bottom + Dpi.S(14);
            btnAdd.SetBounds(pad, row, Dpi.S(92), Dpi.S(38));
            btnEdit.SetBounds(btnAdd.Right + Dpi.S(8), row, Dpi.S(88), Dpi.S(38));
            btnDelete.SetBounds(btnEdit.Right + Dpi.S(8), row, Dpi.S(98), Dpi.S(38));
            btnDown.SetBounds(pad + leftWidth - Dpi.S(40), row, Dpi.S(40), Dpi.S(38));
            btnUp.SetBounds(btnDown.Left - Dpi.S(8) - Dpi.S(40), row, Dpi.S(40), Dpi.S(38));
            tips.SetToolTip(btnUp, "Move up");
            tips.SetToolTip(btnDown, "Move down");
            Controls.AddRange(new Control[] { btnDefaults, list, btnAdd, btnEdit, btnDelete, btnUp, btnDown });
            int leftBottom = btnAdd.Bottom;

            // ---- right column: adapter & preferences ----
            AddHeader("Network adapter", "Which connection should be changed.", rightX, top);
            adapterSelector.SetBounds(rightX, top + Dpi.S(56), rightWidth, Dpi.S(40));
            FillAdapters(adapters, settings.AdapterId);
            Controls.Add(adapterSelector);

            int y = adapterSelector.Bottom + Dpi.S(30);
            AddHeader("Preferences", null, rightX, y);
            tglFlush.SetBounds(rightX, y + Dpi.S(34), rightWidth, Dpi.S(30));
            tglTest.SetBounds(rightX, tglFlush.Bottom + Dpi.S(10), rightWidth, Dpi.S(30));
            tglFlush.Checked = settings.FlushDnsAfterApply;
            tglTest.Checked = settings.TestOnStartup;
            Controls.Add(tglFlush);
            Controls.Add(tglTest);

            y = tglTest.Bottom + Dpi.S(30);
            AddHeader("Appearance", null, rightX, y);
            themeSelector.SetBounds(rightX, y + Dpi.S(34), rightWidth, Dpi.S(38));
            themeSelector.SetItems(new[] { "System", "Dark", "Light" });
            themeSelector.SelectedIndex = (int)settings.Theme;
            themeSelector.SelectedIndexChanged += (s, e) => Theme.Apply((ThemeMode)themeSelector.SelectedIndex);
            Controls.Add(themeSelector);
            int rightBottom = themeSelector.Bottom;

            // ---- footer ----
            separatorY = Math.Max(leftBottom, rightBottom) + Dpi.S(24);
            int footerY = separatorY + Dpi.S(18);

            var btnSave = new ModernButton { Kind = ButtonKind.Primary, Text = "Save" };
            var btnCancel = new ModernButton { Kind = ButtonKind.Secondary, Text = "Cancel", DialogResult = DialogResult.Cancel };
            btnSave.SetBounds(width - pad - Dpi.S(110), footerY, Dpi.S(110), Dpi.S(40));
            btnCancel.SetBounds(btnSave.Left - Dpi.S(10) - Dpi.S(110), footerY, Dpi.S(110), Dpi.S(40));
            btnSave.Click += (s, e) => Save();
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            AcceptButton = btnSave;
            CancelButton = btnCancel;

            var version = typeof(SettingsForm).Assembly.GetName().Version;
            var lblVersion = new ThemedLabel(LabelStyle.Muted) { Text = "DNS Changer " + version.ToString(2) + "  ·", AutoSize = true };
            lblVersion.Location = new Point(pad, footerY + Dpi.S(11));
            var link = new ThemedLink { Text = "GitHub" };
            link.LinkClicked += (s, e) => { try { Process.Start(RepositoryUrl); } catch { } };
            Controls.Add(lblVersion);
            Controls.Add(link);
            link.Location = new Point(lblVersion.Left + lblVersion.PreferredWidth + Dpi.S(2), lblVersion.Top);

            ClientSize = new Size(width, btnSave.Bottom + Dpi.S(20));

            // ---- behaviour ----
            list.SetItems(servers);
            list.SelectedIndexChanged += (s, e) => UpdateButtons();
            list.EntryActivated += (s, e) => EditSelected();
            list.KeyDown += (s, e) => { if (e.KeyCode == Keys.Delete) DeleteSelected(); };
            btnAdd.Click += (s, e) => AddEntry();
            btnEdit.Click += (s, e) => EditSelected();
            btnDelete.Click += (s, e) => DeleteSelected();
            btnUp.Click += (s, e) => MoveSelected(-1);
            btnDown.Click += (s, e) => MoveSelected(1);
            btnDefaults.Click += (s, e) => RestoreDefaults();
            if (servers.Count > 0)
                list.SelectedIndex = 0;
            UpdateButtons();
        }

        public AppSettings Result { get; private set; }

        void AddHeader(string title, string subtitle, int x, int y)
        {
            Controls.Add(new ThemedLabel(LabelStyle.Header) { Text = title, AutoSize = true, Location = new Point(x - Dpi.S(1), y) });
            if (subtitle != null)
                Controls.Add(new ThemedLabel(LabelStyle.Muted) { Text = subtitle, AutoSize = true, Location = new Point(x, y + Dpi.S(26)) });
        }

        void FillAdapters(IList<NetworkAdapter> adapters, string currentId)
        {
            adapterSelector.Items.Add("Automatic — all connected adapters");
            adapterIds.Add(null);
            foreach (var adapter in adapters)
            {
                adapterSelector.Items.Add(adapter.DisplayName);
                adapterIds.Add(adapter.Id);
            }

            int index = adapterIds.FindIndex(id => id != null && string.Equals(id, currentId, StringComparison.OrdinalIgnoreCase));
            if (index < 0 && !string.IsNullOrEmpty(currentId))
            {
                // Keep a saved adapter that is currently disconnected.
                adapterSelector.Items.Add("Saved adapter (not connected)");
                adapterIds.Add(currentId);
                index = adapterIds.Count - 1;
            }
            adapterSelector.SelectedIndex = Math.Max(0, index);
        }

        void UpdateButtons()
        {
            int index = list.SelectedIndex;
            btnEdit.Enabled = btnDelete.Enabled = index >= 0;
            btnUp.Enabled = index > 0;
            btnDown.Enabled = index >= 0 && index < servers.Count - 1;
        }

        void ReloadList(int select)
        {
            list.SetItems(servers);
            list.SelectedIndex = select;
            UpdateButtons();
        }

        void AddEntry()
        {
            using (var editor = new EntryEditorForm(null))
            {
                if (editor.ShowDialog(this) != DialogResult.OK)
                    return;
                servers.Add(editor.Result);
                ReloadList(servers.Count - 1);
            }
        }

        void EditSelected()
        {
            int index = list.SelectedIndex;
            if (index < 0)
                return;
            using (var editor = new EntryEditorForm(servers[index]))
            {
                if (editor.ShowDialog(this) != DialogResult.OK)
                    return;
                servers[index] = editor.Result;
                ReloadList(index);
            }
        }

        void DeleteSelected()
        {
            int index = list.SelectedIndex;
            if (index < 0)
                return;
            servers.RemoveAt(index);
            ReloadList(Math.Min(index, servers.Count - 1));
        }

        void MoveSelected(int delta)
        {
            int index = list.SelectedIndex;
            int target = index + delta;
            if (index < 0 || target < 0 || target >= servers.Count)
                return;
            var entry = servers[index];
            servers[index] = servers[target];
            servers[target] = entry;
            ReloadList(target);
        }

        void RestoreDefaults()
        {
            servers.Clear();
            servers.AddRange(DefaultServers.Create());
            ReloadList(0);
        }

        void Save()
        {
            Result = new AppSettings
            {
                Servers = servers,
                AdapterId = adapterIds[Math.Max(0, adapterSelector.SelectedIndex)],
                FlushDnsAfterApply = tglFlush.Checked,
                TestOnStartup = tglTest.Checked,
                Theme = (ThemeMode)themeSelector.SelectedIndex,
            };
            DialogResult = DialogResult.OK;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(Theme.Current.Border))
                e.Graphics.DrawLine(pen, Dpi.S(24), separatorY, ClientSize.Width - Dpi.S(24), separatorY);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (DialogResult != DialogResult.OK)
                Theme.Apply(originalTheme); // undo live preview
            tips.Dispose();
            base.OnFormClosed(e);
        }
    }
}
