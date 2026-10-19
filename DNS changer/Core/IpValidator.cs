using System.Net;
using System.Net.Sockets;

namespace DNS_changer.Core
{
    public static class IpValidator
    {
        /// <summary>Strict dotted-quad IPv4 check (IPAddress.TryParse alone accepts "1" or "1.2").</summary>
        public static bool IsIPv4(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            value = value.Trim();
            IPAddress address;
            return value.Split('.').Length == 4
                && IPAddress.TryParse(value, out address)
                && address.AddressFamily == AddressFamily.InterNetwork;
        }
    }
}
