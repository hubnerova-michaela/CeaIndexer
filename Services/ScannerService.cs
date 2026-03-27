using CeaIndexer.Data;
using CeaIndexer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CeaIndexer.Services
{

    public class ScanProgressReport
    {
        public enum ReportType { FileStarted, FileCompleted, FileError, MeasurePointFound }

        public ReportType Type { get; set; }
        public string FilePath { get; set; }
        public int ProcessedFiles { get; set; }
        public int TotalFiles { get; set; }
        public string ErrorMessage { get; set; }
        public MeasurePoint ParsedMeasurePoint { get; set; }
    }

    public class ScannerService
    {

        private readonly string _erxPath;

        public ScannerService()
        {

            _erxPath = Properties.Settings.Default.ErxPath;


            if (string.IsNullOrWhiteSpace(_erxPath))
            {
                throw new InvalidOperationException("Cesta k erx.exe není nastavena. Přejděte do nastavení aplikace.");
            }
        }

        
        public async Task<List<FileEntry>> ProcessFilesAsync(List<string> filesToProcess, IProgress<ScanProgressReport> progress = null)
        {
            //kontrola bd
            using (var setupDb = new AppDbContext())
            {
                setupDb.Database.EnsureCreated();
            }

            var results = new List<FileEntry>();
            int totalFiles = filesToProcess.Count;
            int processedFiles = 0;

            foreach (string filePath in filesToProcess)
            {

                progress?.Report(new ScanProgressReport { Type = ScanProgressReport.ReportType.FileStarted, FilePath = filePath });


                try
                {
                    var fileEntry = new FileEntry
                    {
                        FileName = Path.GetFileName(filePath),
                        Path = filePath,
                        ScannedAt = DateTime.Now
                    };


                    // 1: získat všechny Records
                    string listRecordsArgs = $"source=\"{filePath}\" -list-records";
                    string listRecordsText = await RunErxAsync(listRecordsArgs);
                    fileEntry.MeasurePoints = ParseListRecords(listRecordsText);


                    // 2: zkusit najít detailní info
                    string identifyArgs = $"source=\"{filePath}\" -identify";
                    string identifyText = await RunErxAsync(identifyArgs);
                    var identifiedPoints = ParseIdentify(identifyText); 

                    foreach (var mp in fileEntry.MeasurePoints)
                    {

                        var extraInfo = identifiedPoints.FirstOrDefault(x => x.Guid == mp.Guid);
                        if (extraInfo != null)
                        {
                            if (!string.IsNullOrWhiteSpace(extraInfo.Name)) mp.Name = extraInfo.Name;
                            mp.DeviceType = extraInfo.DeviceType;
                            mp.SerialNumber = extraInfo.SerialNumber;
                        }


                        // stahujeme detaily pro tento přístroj
                        progress?.Report(new ScanProgressReport { Type = ScanProgressReport.ReportType.FileStarted, FilePath = filePath });



                        // 3: stáhnout konfigu pro konkrétní GUID
                        string configArgs = $"source=\"{filePath}\" records=\"GUID:{mp.Guid}\" -list-configs";
                        string configText = await RunErxAsync(configArgs);


                        // rozparsovat technická data
                        ParseConfig(configText, mp);


                        progress?.Report(new ScanProgressReport { Type = ScanProgressReport.ReportType.MeasurePointFound, FilePath = filePath, ParsedMeasurePoint = mp });

                        await Task.Delay(50);
                    }

                    using (var db = new AppDbContext())
                    {
                        var existingFile = await db.Files.Include(f => f.MeasurePoints).FirstOrDefaultAsync(f => f.Path == fileEntry.Path);
                        if (existingFile != null)
                        {
                            db.Files.Remove(existingFile);
                            await db.SaveChangesAsync();
                        }

                        db.Files.Add(fileEntry);
                        await db.SaveChangesAsync();
                    }


                    results.Add(fileEntry);
                    processedFiles++;


                    progress?.Report(new ScanProgressReport { Type = ScanProgressReport.ReportType.FileCompleted, FilePath = filePath, ProcessedFiles = processedFiles, TotalFiles = totalFiles });
                }

                catch (Exception ex)
                {
                    processedFiles++;


                    string errorDetail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                    progress?.Report(new ScanProgressReport
                    {
                        Type = ScanProgressReport.ReportType.FileError,
                        FilePath = filePath,
                        ErrorMessage = errorDetail,
                        ProcessedFiles = processedFiles,
                        TotalFiles = totalFiles
                    });
                }
                
            }

            return results;
        }


        private async Task<string> RunErxAsync(string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _erxPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            // Tvoje super-moderní asynchronní čtení!
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return output;
        }

        private List<MeasurePoint> ParseListRecords(string text)
        {
            var points = new List<MeasurePoint>();

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {

                if (line.StartsWith("Datasource:") || line.StartsWith("Guid")) continue;

                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 3)
                {
                    string guid = parts[0];
                    string type = parts[1];


                    string path = string.Join(" ", parts.Skip(2));

                    if (type.Equals("Record", StringComparison.OrdinalIgnoreCase))
                    {
                        var mp = new MeasurePoint
                        {
                            Guid = guid,
                            ObjectPath = path,
                            Name = path.Contains("/") ? path.Substring(path.LastIndexOf('/') + 1) : path
                        };
                        points.Add(mp);
                    }
                }
            }
            return points;
        }


        private List<MeasurePoint> ParseIdentify(string text)
        {
            var points = new List<MeasurePoint>();

            string[] blocks = text.Split(new[] { "Identify:" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var block in blocks)
            {
                if (!block.Contains("Record GUID:")) continue;

                var mp = new MeasurePoint();

                mp.Guid = ExtractValue(block, @"Record GUID:\s*(.+)");
                mp.Name = ExtractValue(block, @"Record Name:\s*(.+)");
                mp.DeviceType = ExtractValue(block, @"Model:\s*(.+)");
                mp.SerialNumber = ExtractValue(block, @"Serial Number:\s*(.+)");
                mp.ObjectPath = ExtractValue(block, @"Object:\s*(.+)");


                if (!string.IsNullOrWhiteSpace(mp.Guid))
                {
                    points.Add(mp);
                }
            }

            return points;
        }

        private string ExtractValue(string textBlock, string regexPattern)
        {
            var match = Regex.Match(textBlock, regexPattern);
            if (match.Success)
            {
                // Vezme text za dvojtečkou a mezerami a ořízne ho o případné mezery na konci
                return match.Groups[1].Value.Trim();
            }
            return string.Empty;
        }

        private void ParseConfig(string configText, MeasurePoint mp)
        {
            var match = Regex.Match(configText, @"Device Installation\r?\n(.*?)(?:\r?\n\r?\n|$)", RegexOptions.Singleline);

            if (match.Success)
            {
                string devInstLine = match.Groups[1].Value.Trim();
                var parts = devInstLine.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    var kv = part.Split(new[] { ": " }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (kv.Length == 2)
                    {
                        string key = kv[0].Trim();
                        string value = kv[1].Trim();

                        switch (key)
                        {
                            case "Type":
                                mp.WiringType = value;
                                break;
                            case "cMode":
                                mp.ConnectionMode = value;
                                break;
                            case "Fnom":
                                mp.Fnom = ParseDoubleWithUnit(value);
                                break;
                            case "Unom":
                                mp.Unom = ParseDoubleWithUnit(value);
                                break;
                            case "Primary CT":
                                mp.PrimaryCT = ParseDoubleWithUnit(value);
                                break;
                            case "Secondary CT":
                                mp.SecondaryCT = ParseDoubleWithUnit(value);
                                break;
                            case "Primary VT":
                                mp.PrimaryVT = ParseDoubleWithUnit(value);
                                break;
                            case "Secondary VT":
                                mp.SecondaryVT = ParseDoubleWithUnit(value);
                                break;
                        }
                    }
                }
            }
        }

        private double? ParseDoubleWithUnit(string input)
        {
            var match = Regex.Match(input, @"[\d,\.]+");
            if (match.Success)
            {
                string numberStr = match.Value.Replace(",", ".");
                if (double.TryParse(numberStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result))
                {
                    return result;
                }
            }
            return null;
        }
    }


}
