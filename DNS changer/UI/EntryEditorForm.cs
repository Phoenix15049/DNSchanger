using System;
using System.Drawing;
using System.Windows.Forms;
using DNS_changer.Core;
using DNS_changer.Models;
using DNS_changer.UI.Controls;

namespace DNS_changer.UI
{
    internal sealed class EntryEditorForm : ThemedForm
    {
        readonly ModernTextBox txtName = new ModernTextBox { Placeholder = "e.g. My DNS" };
        readonly ModernTextBox txtPrimary = new ModernTextBox { Placeholder = "e.g. 1.1.1.1" };
        readonly ModernTextBox txtSecondary = new ModernTextBox { Placeholder = "e.g. 1.0.0.1" };
        readonly SegmentedControl category = new SegmentedControl();
        readonly ThemedLabel lblError = new ThemedLabel(LabelStyle.Error) { AutoEllipsis = true };

        public EntryEditorForm(DnsEntry entry)
        {
            bool isNew = entry == null;
            Result = entry?.Clone() ?? new DnsEntry { Category = DnsCategory.Custom };

            Text = isNew ? "Add DNS server" : "Edit DNS server";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            int pad = Dpi.S(24), width = Dpi.S(400), inner = width - pad * 2;
            int y = Dpi.S(20);
            y = AddField("Name", txtName, pad, y, inner);
            y = AddField("Primary DNS", txtPrimary, pad, y, inner);
            y = AddField("Secondary DNS (optional)", txtSecondary, pad, y, inner);

            Controls.Add(new ThemedLabel { Text = "Category", AutoSize = true, Location = new Point(pad, y) });
            category.SetBounds(pad, y + Dpi.S(24), inner, Dpi.S(38));
            category.SetItems(DnsCategory.All, DnsCategory.Normalize(Result.Category));
            Controls.Add(category);
            y = category.Bottom + Dpi.S(12);

            lblError.SetBounds(pad, y, inner, Dpi.S(20));
            Controls.Add(lblError);
            y = lblError.Bottom + Dpi.S(14);

            var btnSave = new ModernButton { Kind = ButtonKind.Primary, Text = isNew ? "Add" : "Save" };
            var btnCancel = new ModernButton { Kind = ButtonKind.Secondary, Text = "Cancel", DialogResult = DialogResult.Cancel };
            btnSave.SetBounds(width - pad - Dpi.S(100), y, Dpi.S(100), Dpi.S(40));
            btnCancel.SetBounds(btnSave.Left - Dpi.S(10) - Dpi.S(100), y, Dpi.S(100), Dpi.S(40));
            btnSave.Click += (s, e) => Save();
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            AcceptButton = btnSave;
            CancelButton = btnCancel;

            ClientSize = new Size(width, btnSave.Bottom + Dpi.S(20));

            txtName.Text = Result.Name ?? "";
            txtPrimary.Text = Result.Primary ?? "";
            txtSecondary.Text = Result.Secondary ?? "";
            Shown += (s, e) => txtName.FocusInput();
        }

        public DnsEntry Result { get; }

        int AddField(string caption, ModernTextBox box, int x, int y, int width)
        {
            Controls.Add(new ThemedLabel { Text = caption, AutoSize = true, Location = new Point(x, y) });
            box.SetBounds(x, y + Dpi.S(24), width, Dpi.S(40));
            box.TextChanged += (s, e) => { box.HasError = false; lblError.Text = ""; };
            Controls.Add(box);
            return box.Bottom + Dpi.S(14);
        }

        void Save()
        {
            string name = txtName.Text.Trim();
            string primary = txtPrimary.Text.Trim();
            string secondary = txtSecondary.Text.Trim();

            if (name.Length == 0)
            {
                Fail(txtName, "Please enter a name.");
                return;
            }
            if (!IpValidator.IsIPv4(primary))
            {
                Fail(txtPrimary, "Primary DNS must be a valid IPv4 address.");
                return;
            }
            if (secondary.Length > 0 && !IpValidator.IsIPv4(secondary))
            {
                Fail(txtSecondary, "Secondary DNS must be a valid IPv4 address.");
                return;
            }

            Result.Name = name;
            Result.Primary = primary;
            Result.Secondary = secondary.Length > 0 ? secondary : null;
            Result.Category = category.SelectedItem ?? DnsCategory.Custom;
            DialogResult = DialogResult.OK;
        }

        void Fail(ModernTextBox box, string message)
        {
            lblError.Text = message;
            box.HasError = true;
            box.FocusInput();
        }
    }
}
