using CeaIndexer.Data;
using CeaIndexer.FilterModels;
using CeaIndexer.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Globalization;


namespace CeaIndexer.Services
{
    public class SearchEngineService
    {
        private readonly AppDbContext _dbContext;

        public SearchEngineService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        public async Task<List<string>> ExecuteSearchAsync(ConditionGroup rootCondition, List<QuantityCondition> selectedQuantities)
        {
            bool requiresDeepSearch = CheckIfErxIsNeeded(rootCondition) ||
                (selectedQuantities != null && selectedQuantities.Any(q =>
                    !string.IsNullOrWhiteSpace(q.Value) ||
                    q.IntervalStart.HasValue ||
                    q.IntervalEnd.HasValue
                ));

            List<string> candidateFiles = await RunMetadataSearchAsync(rootCondition);


            if (selectedQuantities != null && selectedQuantities.Any() && candidateFiles.Any())
            {
                candidateFiles = await FilterFilesByAvailableQuantitiesAsync(candidateFiles, selectedQuantities);
            }

            if (requiresDeepSearch && candidateFiles.Any())
            {
                candidateFiles = await RunDeepSearchAsync(candidateFiles, rootCondition, selectedQuantities);
            }

            return candidateFiles;
        }


        private async Task<List<string>> FilterFilesByAvailableQuantitiesAsync(List<string> candidateFiles, List<QuantityCondition> selectedQuantities)
        {
            if (candidateFiles == null || !candidateFiles.Any())
                return new List<string>();

            var techNames = selectedQuantities.Select(q => q.TechName).Distinct().ToList();

            var fileData = await _dbContext.Files
                .Where(f => candidateFiles.Contains(f.Path))
                .Where(f => f.MeasurePoints.Any(mp => mp.Archives.Any(a => a.Quantities.Any(q => techNames.Contains(q.Name)))))
                .Select(f => new
                {
                    Path = f.Path,
                    Archives = f.MeasurePoints.SelectMany(mp => mp.Archives)
                                .Where(a => a.Quantities.Any(q => techNames.Contains(q.Name)))
                                .Select(a => new
                                {
                                    StartTime = a.StartTime, 
                                    EndTime = a.EndTime,  
                                    Quantities = a.Quantities.Where(q => techNames.Contains(q.Name)).Select(q => q.Name)
                                })
                })
                .ToListAsync();

            var validFiles = new List<string>();


            foreach (var file in fileData)
            {
                bool filePassedPhase2 = false;

                foreach (var archive in file.Archives)
                {
                    foreach (var cond in selectedQuantities)
                    {
                        if (archive.Quantities.Contains(cond.TechName))
                        {
                            bool timeOverlap = true;

                            if (cond.IntervalStart.HasValue || cond.IntervalEnd.HasValue)
                            {

                                DateTime aStart = archive.StartTime;
                                DateTime aEnd = archive.EndTime;

                                if (cond.IntervalStart.HasValue && aEnd < cond.IntervalStart.Value) timeOverlap = false;
                                if (cond.IntervalEnd.HasValue && aStart > cond.IntervalEnd.Value) timeOverlap = false;
                            }

                            if (timeOverlap)
                            {
                                filePassedPhase2 = true;
                                break;
                            }
                        }
                    }
                    if (filePassedPhase2) break;
                }

                if (filePassedPhase2)
                {
                    validFiles.Add(file.Path);
                }
            }

            return validFiles;
        }



        private bool CheckIfErxIsNeeded(IConditionNode node)
        {
            if (node is ConditionRule rule)
            {
                return rule.Category == RuleCategory.Quantity &&
                       rule.Operator != RelationalOperator.Exists &&
                       rule.Operator != RelationalOperator.NotExists;
            }
            else if (node is ConditionGroup group)
            {
                return group.Children.Any(child => CheckIfErxIsNeeded(child));
            }
            return false;
        }

        private async Task<List<string>> RunMetadataSearchAsync(ConditionGroup rootCondition)
        {
            var dynamicWhereClause = MetadataQueryBuilder.BuildQuery(rootCondition);

            var candidateFiles = await _dbContext.Files
                .Where(dynamicWhereClause)
                .Select(f => f.Path)
                .ToListAsync();

            return candidateFiles;
        }

        private async Task<List<string>> RunDeepSearchAsync(List<string> candidateFiles, ConditionGroup rootCondition, List<QuantityCondition> selectedQuantities)
        {
            var matchedFiles = new System.Collections.Concurrent.ConcurrentBag<string>();
            string erxPath = Properties.Settings.Default.ErxPath;
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 4 };

            if (!System.IO.File.Exists(erxPath))
            {
                System.Windows.MessageBox.Show($"Kritická chyba: Nástroj ERX nebyl nalezen na cestě:\n'{erxPath}'\n\nZkontroluj nastavení aplikace!", "ERX nenalezen", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return candidateFiles;
            }

 
            var allQuantitiesToExport = new HashSet<string>();

            if (selectedQuantities != null)
            {
                foreach (var q in selectedQuantities) allQuantitiesToExport.Add(q.TechName);
            }

            var quantityRules = GetQuantityRules(rootCondition);
            foreach (var rule in quantityRules)
            {
                allQuantitiesToExport.Add(rule.TargetProperty);
            }

            if (!allQuantitiesToExport.Any()) return candidateFiles;

            string quantitiesArg = string.Join(";", allQuantitiesToExport);


            string globalIntervalArg = ExtractIntervalFromConditions(rootCondition);
            string finalIntervalArg = globalIntervalArg;

            if (selectedQuantities != null && selectedQuantities.Any())
            {
                var starts = selectedQuantities.Where(q => q.IntervalStart.HasValue).Select(q => q.IntervalStart.Value).ToList();
                var ends = selectedQuantities.Where(q => q.IntervalEnd.HasValue).Select(q => q.IntervalEnd.Value).ToList();

                if (starts.Any() && ends.Any())
                {
                    finalIntervalArg = $"{starts.Min():yyyy-MM-ddTHH:mm:ss}-{ends.Max():yyyy-MM-ddTHH:mm:ss}";
                }
                else if (starts.Any())
                {
                    finalIntervalArg = $"{starts.Min():yyyy-MM-ddTHH:mm:ss}";
                }
            }

            await Parallel.ForEachAsync(candidateFiles, parallelOptions, async (filePath, cancellationToken) =>
            {
                string tempCsvPath = Path.Combine(Path.GetTempPath(), $"erx_temp_{Guid.NewGuid()}.csv");

                try
                {
                    string arguments = $"source=\"{filePath}\" dest=\"{tempCsvPath}\" quantities=\"{quantitiesArg}\" timeout=30";

                    if (!string.IsNullOrEmpty(finalIntervalArg))
                    {
                        arguments += $" interval=\"{finalIntervalArg}\"";
                    }

                    // --- VÝPIS DO LOGU ---
                    System.Diagnostics.Debug.WriteLine($"[ERX PŘÍKAZ] Volám: \"{erxPath}\" {arguments}");
                    // ---------------------

                    var processInfo = new ProcessStartInfo
                    {
                        FileName = erxPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(processInfo))
                    {
                        if (process != null)
                            await process.WaitForExitAsync(cancellationToken);
                    }

                    if (File.Exists(tempCsvPath))
                    {
                        // ANALÝZA CSV
                        bool conditionMet = AnalyzeCsvFile(tempCsvPath, filePath, rootCondition, quantityRules, selectedQuantities);

                        if (conditionMet)
                        {
                            matchedFiles.Add(filePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Chyba u souboru {filePath}: {ex.Message}");
                }
                finally
                {
                    if (File.Exists(tempCsvPath)) File.Delete(tempCsvPath);
                }
            });

            return matchedFiles.ToList();
        }


        private bool AnalyzeCsvFile(string csvPath, string originalFilePath, ConditionGroup rootCondition, List<ConditionRule> quantityRules, List<QuantityCondition> selectedQuantities)
        {
            using (var reader = new StreamReader(csvPath))
            {
                string line;
                List<string> headers = null;
                string firstDataLine = null;

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("Record;Record Time"))
                    {
                        headers = line.Split(';').Select(h => h.Trim()).ToList();
                    }
                    else if (headers != null && !string.IsNullOrWhiteSpace(line) && !line.StartsWith(";"))
                    {
                        firstDataLine = line;
                        break;
                    }
                }

                if (headers == null) return false;

                int timeColumnIndex = headers.IndexOf("Record Time");


                var cleanHeaders = headers.Select(h => {
                    int bracketIdx = h.IndexOf('[');
                    return bracketIdx > 0 ? h.Substring(0, bracketIdx).Trim() : h.Trim();
                }).ToList();

                var selectedQuantColumnMap = new Dictionary<QuantityCondition, int>();
                if (selectedQuantities != null && selectedQuantities.Any())
                {
                    foreach (var sq in selectedQuantities)
                    {
                        string baseName = sq.TechName.Split('_').Last();
                        int colIndex = cleanHeaders.FindIndex(h => h == baseName || h.EndsWith("." + baseName));
                        if (colIndex != -1) selectedQuantColumnMap[sq] = colIndex;
                    }

                    if (selectedQuantColumnMap.Count == 0) return false;
                }

                var ruleColumnMap = new Dictionary<ConditionRule, int>();
                if (quantityRules != null)
                {
                    foreach (var rule in quantityRules)
                    {
                        int colIndex = cleanHeaders.IndexOf(rule.TargetProperty);
                        if (colIndex != -1) ruleColumnMap[rule] = colIndex;
                    }
                }


                string currentLine = firstDataLine;
                while (currentLine != null)
                {
                    if (!string.IsNullOrWhiteSpace(currentLine))
                    {
                        string[] rowValues = currentLine.Split(';');

                        bool metadataMatch = EvaluateRow(rootCondition, rowValues, ruleColumnMap);

                        bool quantitiesMatch = false;
                        if (!selectedQuantColumnMap.Any())
                        {
                            quantitiesMatch = true;
                        }
                        else
                        {
                            foreach (var kvp in selectedQuantColumnMap)
                            {
                                if (IsConditionMetForRow(kvp.Key, rowValues, kvp.Value, timeColumnIndex))
                                {
                                    quantitiesMatch = true;
                                    break;
                                }
                            }
                        }

                        if (metadataMatch && quantitiesMatch)
                        {
                            string matchTime = (timeColumnIndex != -1 && timeColumnIndex < rowValues.Length)
                                ? rowValues[timeColumnIndex] : "Neznámý čas";

                            string matchedValues = "";
                            foreach (var kvp in selectedQuantColumnMap)
                            {
                                if (kvp.Value < rowValues.Length)
                                {
                                    
                                    matchedValues += $"{kvp.Key.TechName}: {rowValues[kvp.Value]} ";
                                }
                            }

                            string fileName = System.IO.Path.GetFileName(originalFilePath);
                            System.Diagnostics.Debug.WriteLine($"[SHODA NALEZENA] Soubor: {fileName} | Čas: {matchTime} | Naměřené hodnoty: {matchedValues}");

                            return true;
                        }
                    }
                    currentLine = reader.ReadLine();
                }
            }
            return false;
        }


        private bool IsConditionMetForRow(QuantityCondition cond, string[] rowValues, int valColIdx, int timeColIdx)
        {
            if (valColIdx >= rowValues.Length) return false;

            // 1. KONTROLA ČASU (pokud je nastaven interval)
            if (cond.IntervalStart.HasValue || cond.IntervalEnd.HasValue)
            {
                if (timeColIdx != -1 && timeColIdx < rowValues.Length)
                {
                    if (DateTime.TryParse(rowValues[timeColIdx], out DateTime rowTime))
                    {
                        if (cond.IntervalStart.HasValue && rowTime < cond.IntervalStart.Value) return false;
                        if (cond.IntervalEnd.HasValue && rowTime > cond.IntervalEnd.Value) return false;
                    }
                }
            }

            // 2. KONTROLA HODNOTY (Prahová událost)
            if (cond.Operator.HasValue && !string.IsNullOrEmpty(cond.Value))
            {
                if (double.TryParse(rowValues[valColIdx].Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double rowValue) &&
                    double.TryParse(cond.Value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double targetValue))
                {
                    switch (cond.Operator.Value)
                    {
                        case RelationalOperator.Equals: return rowValue == targetValue;
                        case RelationalOperator.GreaterThan: return rowValue > targetValue;
                        case RelationalOperator.LessThan: return rowValue < targetValue;

                    }
                }
                return false;
            }

            return true;
        }

        private List<ConditionRule> GetQuantityRules(IConditionNode node)
        {
            var rules = new List<ConditionRule>();
            if (node is ConditionRule rule && rule.Category == RuleCategory.Quantity)
                rules.Add(rule);
            else if (node is ConditionGroup group)
                foreach (var child in group.Children)
                    rules.AddRange(GetQuantityRules(child));

            return rules;
        }

        private string ExtractIntervalFromConditions(IConditionNode rootNode)
        {


            return null;
        }

        private bool EvaluateRow(ConditionGroup group, string[] rowValues, Dictionary<ConditionRule, int> ruleColumnMap)
        {
            bool isAnd = group.LogicOperator == LogicalOperator.And;
            bool groupResult = isAnd;

            foreach (var child in group.Children)
            {
                bool childResult = false;

                if (child is ConditionGroup subGroup)
                {
                    childResult = EvaluateRow(subGroup, rowValues, ruleColumnMap);
                }
                else if (child is ConditionRule rule)
                {
                    if (rule.Category != RuleCategory.Quantity)
                    {
                        childResult = true;
                    }
                    else
                    {
                        int colIndex = ruleColumnMap[rule];
                        string cellValueText = rowValues[colIndex];

                        if (double.TryParse(cellValueText, NumberStyles.Any, CultureInfo.InvariantCulture, out double cellValue) &&
                            double.TryParse(rule.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double ruleValue))
                        {
                            switch (rule.Operator)
                            {
                                case RelationalOperator.Equals: childResult = cellValue == ruleValue; break;
                                case RelationalOperator.GreaterThan: childResult = cellValue > ruleValue; break;
                                case RelationalOperator.LessThan: childResult = cellValue < ruleValue; break;
                            }
                        }
                    }
                }

                if (isAnd)
                {
                    groupResult = groupResult && childResult;
                    if (!groupResult) break;
                }
                else
                {
                    groupResult = groupResult || childResult;
                    if (groupResult) break;
                }
            }
            return groupResult;
        }



        public async Task<Dictionary<string, List<string>>> GetAutocompleteDataAsync()
        {
            var cache = new Dictionary<string, List<string>>();

            try
            {
                // 1. Data z tabulky MeasurePoints
                cache["DeviceType"] = await _dbContext.MeasurePoints
                    .Where(m => m.DeviceType != null).Select(m => m.DeviceType).Distinct().ToListAsync();

                cache["SerialNumber"] = await _dbContext.MeasurePoints
                    .Where(m => m.SerialNumber != null).Select(m => m.SerialNumber).Distinct().ToListAsync();

                cache["Name"] = await _dbContext.MeasurePoints
                    .Where(m => m.Name != null).Select(m => m.Name).Distinct().ToListAsync();

                cache["WiringType"] = await _dbContext.MeasurePoints
                    .Where(m => m.WiringType != null).Select(m => m.WiringType).Distinct().ToListAsync();

                cache["ConnectionMode"] = await _dbContext.MeasurePoints
                    .Where(m => m.ConnectionMode != null).Select(m => m.ConnectionMode).Distinct().ToListAsync();

                cache["ObjectPath"] = await _dbContext.MeasurePoints
                    .Where(m => m.ObjectPath != null).Select(m => m.ObjectPath).Distinct().ToListAsync();

                // Data z tabulky Files
                cache["FileName"] = await _dbContext.Files
                    .Where(f => f.FileName != null).Select(f => f.FileName).Distinct().ToListAsync();

                cache["Path"] = await _dbContext.Files
                    .Where(f => f.Path != null).Select(f => f.Path).Distinct().ToListAsync();


                // Data z tabulky Archives
                cache["ArchiveName"] = await _dbContext.Archives
                    .Where(a => a.ArchiveName != null).Select(a => a.ArchiveName).Distinct().ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Chyba při načítání dat pro našeptávač z DB: {ex.Message}");
            }

            return cache;
        }

    }

}
