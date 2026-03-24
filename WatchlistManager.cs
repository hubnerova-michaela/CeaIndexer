using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CeaIndexer
{
    public static class WatchlistManager
    {
        private static readonly string WatchlistFile = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "watchlist.txt");

        public static string[] LoadWatchlist()
        {
            try
            {
                if (File.Exists(WatchlistFile))
                {
                    var content = File.ReadAllText(WatchlistFile).Trim();
                    if (!string.IsNullOrEmpty(content))
                    {
                        return content.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                }
            }
            catch
            {
                Logger.Log("Failed to load watchlist");
            }

            return Array.Empty<string>();
        }

        public static void SaveWatchlist(string[] quantities)
        {
            try
            {
                var content = string.Join(";", quantities);
                File.WriteAllText(WatchlistFile, content);
                Logger.Log($"Watchlist saved: {quantities.Length} quantities");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to save watchlist", ex);
            }
        }

        public static bool IsEmpty()
        {
            try
            {
                if (!File.Exists(WatchlistFile))
                    return true;

                var content = File.ReadAllText(WatchlistFile).Trim();
                return string.IsNullOrEmpty(content);
            }
            catch
            {
                return true;
            }
        }
    }
}
