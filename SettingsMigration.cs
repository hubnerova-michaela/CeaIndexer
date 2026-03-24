using System;

namespace CeaIndexer
{
    public static class SettingsMigration
    {
        public const int CURRENT_VERSION = 1;

        public static void MigrateIfNeeded(AppSettings settings)
        {
            try
            {
                if (settings == null)
                    return;

                int settingsVersion = 0;

                if (settings is IVersionable versionable)
                    settingsVersion = versionable.Version;

                if (settingsVersion < CURRENT_VERSION)
                {
                    Logger.Log($"Migrating settings from version {settingsVersion} to {CURRENT_VERSION}");

                    switch (settingsVersion)
                    {
                        case 0:
                            MigrateFromV0(settings);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Settings migration failed", ex);
            }
        }

        private static void MigrateFromV0(AppSettings settings)
        {
            if (string.IsNullOrEmpty(settings.WatchedQuantities))
            {
                settings.WatchedQuantities = "main_U_avg_U1;main_U_avg_U2;main_U_avg_U3;status_temp_core;status_U_nom";
                Logger.Log("Initialized default watchlist from migration");
            }
        }
    }

    public interface IVersionable
    {
        int Version { get; set; }
    }
}
