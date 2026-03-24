using System;
using System.Diagnostics;
using System.IO;

namespace CeaIndexer
{

    public static class EnvisHelper
    {
        public static bool OpenInDefaultApplication(string ceaFilePath)
        {
            try
            {

                if (!File.Exists(ceaFilePath))
                {
                    Logger.LogError($"File not found: {ceaFilePath}", null);
                    return false;
                }

                // Spusù soubor s default aplikacÌ
                var psi = new ProcessStartInfo
                {
                    FileName = ceaFilePath,
                    UseShellExecute = true, 
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    Logger.LogError($"Failed to open file: {ceaFilePath}", null);
                    return false;
                }

                Logger.Log($"Opened file with default application: {ceaFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error opening file: {ex.Message}", ex);
                return false;
            }
        }
    }
}
