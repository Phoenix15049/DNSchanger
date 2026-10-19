using System;
using System.Collections.Generic;

namespace DNS_changer.Models
{
    public static class DnsCategory
    {
        public const string Iran = "Iran";
        public const string Global = "Global";
        public const string Custom = "Custom";

        public static readonly string[] All = { Iran, Global, Custom };

        public static string Normalize(string category)
        {
            foreach (var c in All)
                if (string.Equals(c, category, StringComparison.OrdinalIgnoreCase))
                    return c;
            return string.IsNullOrWhiteSpace(category) ? Custom : category.Trim();
        }
    }

    public class DnsEntry
    {
        public string Name { get; set; }
        public string Primary { get; set; }
        public string Secondary { get; set; }
        public string Category { get; set; }

        public DnsEntry Clone() => (DnsEntry)MemberwiseClone();

        public string[] ToServerArray()
        {
            var list = new List<string> { Primary.Trim() };
            if (!string.IsNullOrWhiteSpace(Secondary))
                list.Add(Secondary.Trim());
            return list.ToArray();
        }

        public string AddressText =>
            string.IsNullOrWhiteSpace(Secondary) ? Primary : Primary + "  ·  " + Secondary;

        /// <summary>True when the adapter's current DNS list corresponds to this entry.</summary>
        public bool Matches(string[] current)
        {
            if (current == null || current.Length == 0 || string.IsNullOrWhiteSpace(Primary))
                return false;
            if (current[0].Trim() != Primary.Trim())
                return false;
            if (string.IsNullOrWhiteSpace(Secondary))
                return true;
            return current.Length > 1 && current[1].Trim() == Secondary.Trim();
        }
    }
}
