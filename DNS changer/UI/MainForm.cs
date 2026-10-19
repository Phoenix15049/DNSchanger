using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;
using DNS_changer.Core;
using DNS_changer.Models;
using DNS_changer.UI.Controls;

namespace DNS_changer.UI
{
    internal sealed class MainForm : ThemedForm
    {
        const string AllTab = "All";

        readonly ThemedLabel lblTitle = new ThemedLabel(LabelStyle.Title) { Text = "DNS Changer", AutoSize = true };
        readonly ThemedLabel lblSubtitle = new ThemedLabel(LabelStyle.Muted) { Text = "Switch DNS servers with one click", AutoSize = true };
        readonly ModernButton btnRefresh = new ModernButton { Kind = ButtonKind.Ghost, Glyph = Glyph.Refresh };
        readonly ModernButton btnSettings = new ModernButton { Kind = ButtonKind.Ghost, Glyph = Glyph.Gear };
        readonly StatusCard status = new StatusCard();
        readonly SegmentedControl tabs = new SegmentedControl();
        readonly DnsListControl list = new DnsListControl();
        readonly ModernButton btnTest = new ModernButton { Kind = ButtonKind.Secondary, Glyph = Glyph.Bolt, Text = "Test speed" };
        readonly ModernButton btnAuto = new ModernButton { Kind = ButtonKind.Secondary, Glyph = Glyph.Reset, Text = "Automatic (DHCP)" };
        readonly ThemedLabel lblMessage = new ThemedLabel(LabelStyle.Muted) { AutoEllipsis = true };
        readonly ToolTip tips = new ToolTip();
        readonly Timer networkTimer = new Timer { Interval = 1500 };

        AppSettings settings;
        List<NetworkAdapter> adapters = new List<NetworkAdapter>();
        List<NetworkAdapter> targets = new List<NetworkAdapter>();
        bool applying, testing;
        int refreshVersion;

        public MainForm(AppSettings settings)
        {
            this.settings = settings;

            Text = "DNS Changer";
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            var work = Screen.PrimaryScreen.WorkingArea;
            ClientSize = new System.Drawing.Size(Dpi.S(460), Math.Min(Dpi.S(680), work.Height - Dpi.S(60)));
            // Width is fixed; only the height can be resized.
            MaximizeBox = false;
            MinimumSize = new System.Drawing.Size(Width, SizeFromClientSize(Dpi.S(460, 540)).Height);
            MaximumSize = new System.Drawing.Size(Width, short.MaxValue);

            btnRefresh.Size = btnSettings.Size = Dpi.S(38, 38);
            tips.SetToolTip(btnRefresh, "Refresh status (F5)");
            tips.SetToolTip(btnSettings, "Settings (Ctrl+,)");
            tips.SetToolTip(btnTest, "Measure the response time of every server");
            tips.SetToolTip(btnAuto, "Let your router / ISP provide DNS");

            Controls.AddRange(new Control[] { lblTitle, lblSubtitle, btnRefresh, btnSettings, status, tabs, list, btnTest, btnAuto, lblMessage });

            btnRefresh.Click += async (s, e) => await RefreshStatusAsync(true);
            btnSettings.Click += (s, e) => OpenSettings();
            btnTest.Click += async (s, e) => await TestAllAsync();
            btnAuto.Click += async (s, e) => await ApplyAsync(null);
            list.EntryActivated += async (s, e) => await ApplyAsync(e.Entry);
            tabs.SelectedIndexChanged += (s, e) => FilterList();
            networkTimer.Tick += async (s, e) =>
            {
                networkTimer.Stop();
                if (!applying)
                    await RefreshStatusAsync(false);
            };
            NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;

            RebuildTabs();
            LayoutControls();
            if (!DnsService.IsElevated)
                ShowMessage("Not running as administrator — DNS changes will fail.", LabelStyle.Warning);
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            list.Focus();
            await RefreshStatusAsync(false);
            if (settings.TestOnStartup)
                await TestAllAsync();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutControls();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.F5)
            {
                e.Handled = true;
                btnRefresh.PerformClick();
            }
            else if (e.Control && e.KeyCode == Keys.Oemcomma)
            {
                e.Handled = true;
                OpenSettings();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
            networkTimer.Dispose();
            tips.Dispose();
            base.OnFormClosed(e);
        }

        void LayoutControls()
        {
            int pad = Dpi.S(20);
            int width = ClientSize.Width - pad * 2;
            int bottom = ClientSize.Height;

            lblTitle.Location = new System.Drawing.Point(pad - Dpi.S(2), Dpi.S(16));
            lblSubtitle.Location = new System.Drawing.Point(pad, Dpi.S(50));
            btnSettings.Location = new System.Drawing.Point(ClientSize.Width - pad - btnSettings.Width, Dpi.S(22));
            btnRefresh.Location = new System.Drawing.Point(btnSettings.Left - Dpi.S(4) - btnRefresh.Width, Dpi.S(22));

            status.SetBounds(pad, Dpi.S(84), width, Dpi.S(100));
            tabs.SetBounds(pad, status.Bottom + Dpi.S(16), width, Dpi.S(38));

            lblMessage.SetBounds(pad, bottom - Dpi.S(34), width, Dpi.S(22));
            int buttonsY = lblMessage.Top - Dpi.S(8) - Dpi.S(40);
            int half = (width - Dpi.S(10)) / 2;
            btnTest.SetBounds(pad, buttonsY, half, Dpi.S(40));
            btnAuto.SetBounds(pad + half + Dpi.S(10), buttonsY, width - half - Dpi.S(10), Dpi.S(40));

            int listTop = tabs.Bottom + Dpi.S(14);
            list.SetBounds(pad, listTop, width, Math.Max(Dpi.S(80), buttonsY - Dpi.S(14) - listTop));
        }

        // ---- list & tabs ----

        void RebuildTabs()
        {
            string current = tabs.SelectedItem;
            var categories = settings.Servers
                .Select(s => DnsCategory.Normalize(s.Category))
                .Distinct()
                .OrderBy(c => { int i = Array.IndexOf(DnsCategory.All, c); return i < 0 ? 99 : i; })
                .ToList();
            categories.Insert(0, AllTab);
            tabs.SetItems(categories, current);
        }

        void FilterList()
        {
            string category = tabs.SelectedItem;
            var entries = category == null || category == AllTab
                ? settings.Servers.ToList()
                : settings.Servers.Where(s => DnsCategory.Normalize(s.Category) == category).ToList();
            list.EmptyText = settings.Servers.Count == 0
                ? "No DNS servers yet — add some in Settings."
                : "No servers in this category.";
            list.SetItems(entries);
        }

        // ---- status ----

        void OnNetworkAddressChanged(object sender, EventArgs e)
        {
            try
            {
                if (IsHandleCreated && !IsDisposed)
                    BeginInvoke((Action)(() => { networkTimer.Stop(); networkTimer.Start(); }));
            }
            catch (InvalidOperationException)
            {
                // Form is closing.
            }
        }

        async Task RefreshStatusAsync(bool userInitiated)
        {
            int version = ++refreshVersion;
            if (userInitiated)
                status.Set(StatusKind.Loading, "Checking…", "", "");

            List<NetworkAdapter> found;
            try
            {
                found = await Task.Run(() => DnsService.GetAdapters());
            }
            catch (Exception ex)
            {
                if (version != refreshVersion)
                    return;
                adapters = new List<NetworkAdapter>();
                targets = new List<NetworkAdapter>();
                status.Set(StatusKind.Offline, "Unable to read network adapters", ex.Message, "");
                return;
            }

            if (version != refreshVersion || IsDisposed)
                return;
            adapters = found;
            targets = DnsService.PickTargets(adapters, settings.AdapterId);
            UpdateStatus();
            if (userInitiated && !applying)
                ShowMessage("Status refreshed.", LabelStyle.Muted);
        }

        void UpdateStatus()
        {
            var shown = targets.FirstOrDefault();
            if (shown == null)
            {
                list.ActiveEntry = null;
                status.Set(StatusKind.Offline, "Not connected", "No active network adapter was found.", "");
                return;
            }

            string adapterText = shown.Name + (targets.Count > 1 ? "  +" + (targets.Count - 1) : "");
            string[] dns = shown.DnsServers;
            string detail = dns.Length > 0 ? string.Join("  ·  ", dns) : "No DNS servers";

            if (shown.IsAutomatic)
            {
                list.ActiveEntry = null;
                status.Set(StatusKind.Automatic, "Automatic (DHCP)", dns.Length > 0 ? detail : "Provided by your router", adapterText);
                return;
            }

            var match = settings.Servers.FirstOrDefault(s => s.Matches(dns));
            list.ActiveEntry = match;
            status.Set(match != null ? StatusKind.Preset : StatusKind.Custom, match != null ? match.Name : "Custom DNS", detail, adapterText);
        }

        // ---- actions ----

        async Task ApplyAsync(DnsEntry entry)
        {
            if (applying)
                return;
            if (targets.Count == 0)
            {
                await RefreshStatusAsync(false);
                if (targets.Count == 0)
                {
                    ShowMessage("No connected network adapter was found.", LabelStyle.Error);
                    return;
                }
            }

            applying = true;
            list.BusyEntry = entry;
            btnAuto.Enabled = false;
            string name = entry?.Name ?? "Automatic DNS";
            ShowMessage("Applying " + name + "…", LabelStyle.Muted);

            var selectedTargets = targets.ToList();
            string[] servers = entry?.ToServerArray();
            bool flush = settings.FlushDnsAfterApply;
            ApplyResult result;
            try
            {
                result = await Task.Run(() => DnsService.Apply(selectedTargets, servers, flush));
            }
            catch (Exception ex)
            {
                result = ApplyResult.Fail(ex.Message);
            }

            applying = false;
            list.BusyEntry = null;
            btnAuto.Enabled = true;
            await RefreshStatusAsync(false);

            if (result.Errors.Count == 0)
            {
                string where = selectedTargets.Count == 1 ? selectedTargets[0].Name : selectedTargets.Count + " adapters";
                ShowMessage("✓  " + name + " is now active on " + where + ".", LabelStyle.Success);
            }
            else if (result.Succeeded > 0)
            {
                ShowMessage("Applied partially. " + result.Errors[0], LabelStyle.Warning);
            }
            else
            {
                ShowMessage("Couldn't change DNS: " + result.Errors[0], LabelStyle.Error);
            }
        }

        async Task TestAllAsync()
        {
            if (testing || settings.Servers.Count == 0)
                return;
            testing = true;
            btnTest.Enabled = false;
            ShowMessage("Testing response time…", LabelStyle.Muted);

            var servers = settings.Servers.Where(s => !string.IsNullOrWhiteSpace(s.Primary)).ToList();
            var results = new Dictionary<DnsEntry, int>();
            foreach (var s in servers)
                list.SetTesting(s.Primary);

            await Task.WhenAll(servers.Select(async s =>
            {
                int ms = await DnsProbe.MeasureAsync(s.Primary);
                results[s] = ms;
                list.SetLatency(s.Primary, ms);
            }));

            testing = false;
            btnTest.Enabled = true;
            var fastest = results.Where(r => r.Value >= 0).OrderBy(r => r.Value).FirstOrDefault();
            if (fastest.Key != null)
                ShowMessage("Fastest: " + fastest.Key.Name + " (" + fastest.Value + " ms). Click a server to use it.", LabelStyle.Success);
            else
                ShowMessage("No server responded. Check your internet connection.", LabelStyle.Error);
        }

        void OpenSettings()
        {
            using (var form = new SettingsForm(settings, adapters))
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                    return;
                settings = form.Result;
            }

            try
            {
                SettingsStore.Save(settings);
                ShowMessage("Settings saved.", LabelStyle.Muted);
            }
            catch (Exception ex)
            {
                ShowMessage("Couldn't save settings: " + ex.Message, LabelStyle.Error);
            }

            RebuildTabs();
            targets = DnsService.PickTargets(adapters, settings.AdapterId);
            UpdateStatus();
        }

        void ShowMessage(string text, LabelStyle style)
        {
            lblMessage.Style = style;
            lblMessage.Font = Theme.Font(9f);
            lblMessage.Text = text;
        }
    }
}
