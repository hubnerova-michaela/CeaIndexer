using System;
using System.IO;

namespace CeaIndexer
{
    public static class Logger
    {
        private static readonly string LogFile = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "app.log");

        private static readonly object _lockObject = new();

        public static void Log(string message)
        {
            try
            {
                lock (_lockObject)
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    var logLine = $"[{timestamp}] {message}";
                    File.AppendAllText(LogFile, logLine + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logger failed: {ex.Message}");
            }
        }

        public static void LogError(string message, Exception ex)
        {
            try
            {
                lock (_lockObject)
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    var logLine = $"[{timestamp}] ERROR: {message}" + Environment.NewLine +
                                  $"         Exception: {ex.GetType().Name}: {ex.Message}" + Environment.NewLine +
                                  $"         StackTrace: {ex.StackTrace}";
                    File.AppendAllText(LogFile, logLine + Environment.NewLine);
                }
            }
            catch (Exception logEx)
            {
                System.Diagnostics.Debug.WriteLine($"Logger failed: {logEx.Message}");
            }
        }

        public static string GetLogFilePath()
        {
            return LogFile;
        }
    }
}
