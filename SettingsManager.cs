using System.IO;
using System.Text.Json;

namespace CeaIndexer
{
    public class SettingsManager
    {
        private static readonly string SettingsFile = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "settings.json");

        private static AppSettings _cachedSettings;

        public static AppSettings Load()
        {
            if (_cachedSettings != null)
                return _cachedSettings;

            try
            {
                if (File.Exists(SettingsFile))
                {
                    var json = File.ReadAllText(SettingsFile);
                    _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json);
                }
            }
            catch
            {
            }

            _cachedSettings ??= new AppSettings();
            return _cachedSettings;
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                _cachedSettings = settings;
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch
            {
            }
        }
    }

    public class AppSettings
    {
        public string ErxExePath { get; set; } = "";
        public string CeaFolderPath { get; set; } = "";
        public string Language { get; set; } = "en";
    }
}
