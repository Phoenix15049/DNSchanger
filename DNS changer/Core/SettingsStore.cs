using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using DNS_changer.Models;

namespace DNS_changer.Core
{
    /// <summary>
    /// Stores settings as XML. A settings.xml next to the exe enables portable mode,
    /// otherwise %AppData%\DNSchanger\settings.xml is used.
    /// </summary>
    public static class SettingsStore
    {
        static readonly XmlSerializer Serializer = new XmlSerializer(typeof(AppSettings));

        static string PortablePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xml");

        static string AppDataPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DNSchanger", "settings.xml");

        public static string FilePath => File.Exists(PortablePath) ? PortablePath : AppDataPath;

        public static AppSettings Load()
        {
            try
            {
                string path = FilePath;
                if (File.Exists(path))
                {
                    var document = XDocument.Load(path);
                    using (var reader = document.CreateReader())
                    {
                        var settings = (AppSettings)Serializer.Deserialize(reader);
                        if (settings != null)
                        {
                            // XmlSerializer always creates the list, so check the file itself.
                            if (settings.Servers == null || document.Root?.Element("Servers") == null)
                                settings.Servers = DefaultServers.Create();
                            foreach (var s in settings.Servers)
                                s.Category = DnsCategory.Normalize(s.Category);
                            return settings;
                        }
                    }
                }
            }
            catch
            {
                // Corrupt or unreadable file: fall back to defaults.
            }
            return AppSettings.CreateDefault();
        }

        public static void Save(AppSettings settings)
        {
            string path = FilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".tmp";
            using (var stream = File.Create(temp))
                Serializer.Serialize(stream, settings);
            if (File.Exists(path))
                File.Delete(path);
            File.Move(temp, path);
        }
    }
}
