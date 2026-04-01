using CeaIndexer.Data;
using CeaIndexer.Helpers;
using CeaIndexer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

                        // časy měření a výpadky (-list-archive-info)

                        string archiveArgs = $"source=\"{filePath}\" records=\"GUID:{mp.Guid}\" -list-archive-info";
                        string archiveText = await RunErxAsync(archiveArgs);
                        ParseArchiveInfo(archiveText, mp);



                        // názvy měřených veličin (-list-quantities)
                        string quantArgs = $"source=\"{filePath}\" records=\"GUID:{mp.Guid}\" -list-quantities";
                        string quantText = await RunErxAsync(quantArgs);
                        ParseQuantities(quantText, mp);


                        progress?.Report(new ScanProgressReport { Type = ScanProgressReport.ReportType.MeasurePointFound, FilePath = filePath, ParsedMeasurePoint = mp });

                        await Task.Delay(50);
                    }

                    foreach (var mp in fileEntry.MeasurePoints)
                    {
                        UnifyDeviceTypeWithSiblings(mp, fileEntry);

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

        private void ParseArchiveInfo(string text, MeasurePoint mp)
        {
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);


            if (string.IsNullOrEmpty(mp.DeviceType) || string.IsNullOrEmpty(mp.SerialNumber))
            {
                GetMeasurePointInfoFromArchiveInfo(lines, mp);
            }

            string pattern = @"^(?<archive>.+?)\s+(?<count>\d+)\s+(?<from>\d{2}\.\d{2}\.\d{4}\s+\d{1,2}:\d{2}:\d{2})\s+(?<to>\d{2}\.\d{2}\.\d{4}\s+\d{1,2}:\d{2}:\d{2})\s+(?<missing>[\d,]+%|-)";

            foreach (var line in lines)
            {
                var match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    string archiveName = match.Groups["archive"].Value.Trim();

                    if (archiveName.Equals("All", StringComparison.OrdinalIgnoreCase)) continue;

                    var archive = new Archive
                    {
                        ArchiveName = archiveName
                    };


                    string[] dateFormats = { "dd.MM.yyyy H:mm:ss", "dd.MM.yyyy HH:mm:ss" };

                    if (DateTime.TryParseExact(match.Groups["from"].Value, dateFormats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime fromDate))
                    {
                        archive.StartTime = fromDate;
                    }

                    if (DateTime.TryParseExact(match.Groups["to"].Value, dateFormats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime toDate))
                    {
                        archive.EndTime = toDate;
                    }

                    // Parsování procent výpadků
                    string missingStr = match.Groups["missing"].Value;
                    if (missingStr != "-" && missingStr.Contains("%"))
                    {
                        missingStr = missingStr.Replace("%", "").Trim();
                        if (double.TryParse(missingStr, NumberStyles.Any, new CultureInfo("cs-CZ"), out double missing))
                        {
                            archive.MissingPercentage = missing;
                        }
                    }

                    mp.Archives.Add(archive);
                }
            }
        }

        private void GetMeasurePointInfoFromArchiveInfo(string[] lines, MeasurePoint mp)
        {

            bool isHeaderFound = false;

            foreach (var line in lines)
            {
                if (line.StartsWith("RECORD") && line.Contains("DEVICE") && line.Contains("SERIAL NUMBER"))
                {
                    isHeaderFound = true;
                    continue;
                }

                if (isHeaderFound)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var columns = Regex.Split(line.Trim(), @"[\s\xA0]{2,}");

                    // [0] RECORD (RM1) | [1] OBJECT (Hlavní) | [2] DEVICE (SIMON S) | [3] SERIAL NUMBER (5) | ...
                    if (columns.Length >= 4)
                    {
                        if (string.IsNullOrEmpty(mp.DeviceType))
                        {
                            mp.DeviceType = columns[2].Trim();
                        }

                        if (string.IsNullOrEmpty(mp.SerialNumber))
                        {
                            mp.SerialNumber = columns[3].Trim();
                        }
                    }

                    break;
                }
            }
        }


        private void UnifyDeviceTypeWithSiblings(MeasurePoint currentMp, FileEntry parentFile)
        {
            if (string.IsNullOrEmpty(currentMp.SerialNumber) || string.IsNullOrEmpty(currentMp.DeviceType))
            {
                return;
            }

            var otherMeasurePoints = parentFile.MeasurePoints.Where(mp => mp != currentMp);

            foreach (var otherMp in otherMeasurePoints)
            {

                if (string.IsNullOrEmpty(otherMp.SerialNumber) || string.IsNullOrEmpty(otherMp.DeviceType))
                {
                    continue;
                }

                bool isSameSerial = string.Equals(currentMp.SerialNumber, otherMp.SerialNumber, StringComparison.OrdinalIgnoreCase);

                bool isSubstringOfLonger = otherMp.DeviceType.StartsWith(currentMp.DeviceType, StringComparison.OrdinalIgnoreCase)
                                        && otherMp.DeviceType.Length > currentMp.DeviceType.Length;

                if (isSameSerial && isSubstringOfLonger)
                {
                    currentMp.DeviceType = otherMp.DeviceType;
                    break;
                }
            }
        }

        private void ParseQuantities(string text, MeasurePoint mp)
        {
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            bool isQuantity = false;

            foreach (var line in lines)
            {
                if (line.Trim() == "QUANTITIES:")
                {
                    isQuantity = true;
                    continue;
                }

                if (isQuantity)
                {
                    string qName = line.Trim();
                    if (!string.IsNullOrWhiteSpace(qName))
                    {
                        // 1. Rozsekneme název pro získání předpony (např. "meter")
                        string archPrefix = qName.Contains("_") ? qName.Substring(0, qName.IndexOf('_')) : "";

                        // 2. Podíváme se do slovníku, jestli známe oficiální název archivu
                        // Pokud předponu známe, vezmeme oficiální název (např. "Electricity Meter"). 
                        // Pokud ne, použijeme samotnou předponu jako zálohu.
                        string expectedArchiveName = ArchiveMapper.GetOfficialName(archPrefix);

                        // 3. Najdeme archiv přesně podle jména! (Žádné StartsWith, ale přesná shoda)
                        var targetArchive = mp.Archives.FirstOrDefault(a => a.ArchiveName.Equals(expectedArchiveName, StringComparison.OrdinalIgnoreCase));

                        // 4. ZÁCHRANNÁ SÍŤ (Když archiv fakt neexistuje)
                        if (targetArchive == null)
                        {
                            targetArchive = new Archive
                            {
                                ArchiveName = string.IsNullOrWhiteSpace(expectedArchiveName) ? "Ostatní (Neznámé)" : expectedArchiveName,
                                StartTime = DateTime.MinValue,
                                EndTime = DateTime.MaxValue,
                                MissingPercentage = null
                            };
                            mp.Archives.Add(targetArchive);
                        }

                        // 5. Přidání veličiny do správného archivu
                        targetArchive.Quantities.Add(new QuantityItem
                        {
                            Name = qName
                        });
                    }
                }
            }
        }
    }


}
