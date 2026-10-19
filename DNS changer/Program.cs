using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using DNS_changer.Core;
using DNS_changer.UI;

namespace DNS_changer
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (new Mutex(true, @"Local\DNSchanger.Phoenix15049", out createdNew))
            {
                if (!createdNew)
                {
                    ActivateRunningInstance();
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException += (s, e) =>
                    MessageBox.Show(e.Exception.Message, "DNS Changer", MessageBoxButtons.OK, MessageBoxIcon.Error);

                Dpi.Init();
                var settings = SettingsStore.Load();
                Theme.Apply(settings.Theme);
                Application.Run(new MainForm(settings));
            }
        }

        static void ActivateRunningInstance()
        {
            var current = Process.GetCurrentProcess();
            foreach (var process in Process.GetProcessesByName(current.ProcessName))
            {
                if (process.Id != current.Id && process.MainWindowHandle != IntPtr.Zero)
                {
                    Native.BringToFront(process.MainWindowHandle);
                    break;
                }
            }
        }
    }
}
