using System;
using System.IO;
using System.Diagnostics;

namespace CeaIndexer
{
    public static class DiagnosticsHelper
    {
        public static void OpenLogFile()
        {
            try
            {
                var logPath = Logger.GetLogFilePath();
                if (File.Exists(logPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "notepad.exe",
                        Arguments = logPath,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to open log file", ex);
            }
        }

        public static void OpenAppDirectory()
        {
            try
            {
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = appDir,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to open app directory", ex);
            }
        }

        public static string GetDiagnosticsInfo()
        {
            try
            {
                var info = $"CeaIndexer Diagnostics - {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                          $"==========================================\n" +
                          $".NET Version: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}\n" +
                          $"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}\n" +
                          $"App Directory: {AppDomain.CurrentDomain.BaseDirectory}\n" +
                          $"Log File: {Logger.GetLogFilePath()}\n" +
                          $"Database: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "index.db")}\n" +
                          $"Watchlist File: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "watchlist.txt")}\n";

                return info;
            }
            catch (Exception ex)
            {
                return $"Error gathering diagnostics: {ex.Message}";
            }
        }
    }
}
