using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;

namespace DNS_changer.Core
{
    public class NetworkAdapter
    {
        public int Index { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsUp { get; set; }
        public bool HasGateway { get; set; }
        public bool IsAutomatic { get; set; }
        public string[] DnsServers { get; set; } = new string[0];

        public string DisplayName =>
            string.IsNullOrEmpty(Description) || Description == Name ? Name : Name + "  —  " + Description;
    }

    public class ApplyResult
    {
        public int Succeeded { get; set; }
        public List<string> Errors { get; } = new List<string>();

        public static ApplyResult Fail(string error)
        {
            var r = new ApplyResult();
            r.Errors.Add(error);
            return r;
        }
    }

    public static class DnsService
    {
        const string InterfacesKey = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\";

        public static bool IsElevated
        {
            get
            {
                try
                {
                    using (var identity = WindowsIdentity.GetCurrent())
                        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>Lists IP-enabled adapters, connected ones with a gateway first.</summary>
        public static List<NetworkAdapter> GetAdapters()
        {
            var nics = new Dictionary<string, NetworkInterface>(StringComparer.OrdinalIgnoreCase);
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                nics[nic.Id] = nic;

            var adapters = new List<NetworkAdapter>();
            const string query = "SELECT Index, SettingID, Description, DNSServerSearchOrder, DefaultIPGateway " +
                                 "FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = TRUE";
            using (var searcher = new ManagementObjectSearcher(query))
            using (var results = searcher.Get())
            {
                foreach (ManagementObject mo in results)
                {
                    using (mo)
                    {
                        var adapter = new NetworkAdapter
                        {
                            Index = Convert.ToInt32(mo["Index"]),
                            Id = mo["SettingID"] as string ?? "",
                            Description = mo["Description"] as string ?? "",
                            DnsServers = (mo["DNSServerSearchOrder"] as string[]) ?? new string[0],
                        };
                        var gateways = mo["DefaultIPGateway"] as string[];
                        adapter.HasGateway = gateways != null && gateways.Any(g => !string.IsNullOrWhiteSpace(g));

                        NetworkInterface nic;
                        if (nics.TryGetValue(adapter.Id, out nic))
                        {
                            adapter.Name = nic.Name;
                            adapter.IsUp = nic.OperationalStatus == OperationalStatus.Up;
                        }
                        else
                        {
                            adapter.Name = adapter.Description;
                            adapter.IsUp = true;
                        }
                        adapter.IsAutomatic = IsDnsAutomatic(adapter.Id);
                        adapters.Add(adapter);
                    }
                }
            }

            return adapters
                .OrderByDescending(a => a.IsUp && a.HasGateway)
                .ThenBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        /// <summary>Adapters that a DNS change should be applied to.</summary>
        public static List<NetworkAdapter> PickTargets(IList<NetworkAdapter> adapters, string preferredId)
        {
            if (!string.IsNullOrEmpty(preferredId))
            {
                var preferred = adapters.Where(a => string.Equals(a.Id, preferredId, StringComparison.OrdinalIgnoreCase)).ToList();
                if (preferred.Count > 0)
                    return preferred;
            }
            var connected = adapters.Where(a => a.IsUp && a.HasGateway).ToList();
            return connected.Count > 0 ? connected : adapters.Where(a => a.IsUp).ToList();
        }

        /// <summary>Sets static DNS servers, or reverts to DHCP when <paramref name="servers"/> is null.</summary>
        public static ApplyResult Apply(IEnumerable<NetworkAdapter> targets, string[] servers, bool flushCache)
        {
            var result = new ApplyResult();
            foreach (var adapter in targets)
            {
                string error;
                if (TrySetWithWmi(adapter, servers, out error) || TrySetWithNetsh(adapter, servers))
                    result.Succeeded++;
                else
                    result.Errors.Add(adapter.Name + ": " + error);
            }
            if (flushCache && result.Succeeded > 0)
                FlushCache();
            return result;
        }

        public static void FlushCache()
        {
            try
            {
                DnsFlushResolverCache();
            }
            catch
            {
                RunHidden("ipconfig", "/flushdns");
            }
        }

        static bool IsDnsAutomatic(string id)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(InterfacesKey + id))
                    return string.IsNullOrWhiteSpace(key?.GetValue("NameServer") as string);
            }
            catch
            {
                return false;
            }
        }

        static bool TrySetWithWmi(NetworkAdapter adapter, string[] servers, out string error)
        {
            error = null;
            try
            {
                using (var mo = new ManagementObject("Win32_NetworkAdapterConfiguration.Index=" + adapter.Index))
                using (var input = mo.GetMethodParameters("SetDNSServerSearchOrder"))
                {
                    input["DNSServerSearchOrder"] = servers;
                    using (var output = mo.InvokeMethod("SetDNSServerSearchOrder", input, null))
                    {
                        uint code = Convert.ToUInt32(output["ReturnValue"]);
                        if (code == 0 || code == 1)
                            return true;
                        error = DescribeWmiError(code);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        static bool TrySetWithNetsh(NetworkAdapter adapter, string[] servers)
        {
            string name = "\"" + adapter.Name + "\"";
            if (servers == null)
                return RunHidden("netsh", "interface ip set dns name=" + name + " source=dhcp");

            if (!RunHidden("netsh", "interface ip set dns name=" + name + " static " + servers[0] + " primary"))
                return false;
            for (int i = 1; i < servers.Length; i++)
                RunHidden("netsh", "interface ip add dns name=" + name + " " + servers[i] + " index=" + (i + 1));
            return true;
        }

        static string DescribeWmiError(uint code)
        {
            switch (code)
            {
                case 70: return "Invalid IP address";
                case 84: return "IP is not enabled on this adapter";
                case 91: return "Access denied — run the app as administrator";
                default: return "Windows returned error " + code;
            }
        }

        static bool RunHidden(string file, string arguments)
        {
            try
            {
                var info = new ProcessStartInfo(file, arguments)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                using (var process = Process.Start(info))
                {
                    process.StandardOutput.ReadToEnd();
                    if (!process.WaitForExit(15000))
                        return false;
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
        static extern uint DnsFlushResolverCache();
    }
}
