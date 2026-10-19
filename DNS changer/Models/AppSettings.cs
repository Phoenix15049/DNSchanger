using System.Collections.Generic;

namespace DNS_changer.Models
{
    public enum ThemeMode
    {
        System,
        Dark,
        Light
    }

    public class AppSettings
    {
        /// <summary>Null when the settings file has no server list, so defaults can be applied.</summary>
        public List<DnsEntry> Servers { get; set; }

        /// <summary>Network adapter GUID to change; empty means every connected adapter.</summary>
        public string AdapterId { get; set; }

        public bool FlushDnsAfterApply { get; set; } = true;
        public bool TestOnStartup { get; set; } = true;
        public ThemeMode Theme { get; set; } = ThemeMode.System;

        public static AppSettings CreateDefault()
        {
            return new AppSettings { Servers = DefaultServers.Create() };
        }
    }

    public static class DefaultServers
    {
        public static List<DnsEntry> Create()
        {
            return new List<DnsEntry>
            {
                // Popular Iranian anti-sanction / gaming resolvers
                Entry("Shecan", "178.22.122.100", "185.51.200.2", DnsCategory.Iran),
                Entry("Electro", "78.157.42.100", "78.157.42.101", DnsCategory.Iran),
                Entry("Begzar", "185.55.226.26", "185.55.225.25", DnsCategory.Iran),
                Entry("403.online", "10.202.10.202", "10.202.10.102", DnsCategory.Iran),
                Entry("Radar Game", "10.202.10.10", "10.202.10.11", DnsCategory.Iran),

                // Fast, privacy-respecting global resolvers
                Entry("Cloudflare", "1.1.1.1", "1.0.0.1", DnsCategory.Global),
                Entry("Google", "8.8.8.8", "8.8.4.4", DnsCategory.Global),
                Entry("Quad9", "9.9.9.9", "149.112.112.112", DnsCategory.Global),
                Entry("AdGuard (ad blocking)", "94.140.14.14", "94.140.15.15", DnsCategory.Global),
            };
        }

        static DnsEntry Entry(string name, string primary, string secondary, string category)
        {
            return new DnsEntry { Name = name, Primary = primary, Secondary = secondary, Category = category };
        }
    }
}
