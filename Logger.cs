using System;
using System.IO;

namespace CeaIndexer
{
    public static class Logger
    {
        private static readonly string LogFile = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "app.log");

        public static void Log(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logLine = $"[{timestamp}] {message}";
                File.AppendAllText(LogFile, logLine + Environment.NewLine);
            }
            catch
            {
            }
        }

        public static void LogError(string message, Exception ex)
        {
            Log($"ERROR: {message} - {ex.Message}");
        }
    }
}
