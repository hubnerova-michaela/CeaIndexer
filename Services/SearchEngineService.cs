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


        public async Task<List<string>> ExecuteSearchAsync(ConditionGroup rootCondition, List<string> selectedQuantities)
        {

            bool requiresDeepSearch = CheckIfErxIsNeeded(rootCondition) || (selectedQuantities != null && selectedQuantities.Any());

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


        private async Task<List<string>> FilterFilesByAvailableQuantitiesAsync(List<string> candidateFiles, List<string> selectedQuantities)
        {
            if (candidateFiles == null || !candidateFiles.Any())
                return new List<string>();


            var validFiles = await _dbContext.Files
                .Where(f => candidateFiles.Contains(f.Path)) 
                .Where(f => f.MeasurePoints.Any(mp => mp.Archives.Any(a =>
                            a.Quantities.Any(q => selectedQuantities.Contains(q.Name)))))
                .Select(f => f.Path)
                .ToListAsync();

            return validFiles;
        }

        // --- POMOCNÉ METODY ---
        private bool CheckIfErxIsNeeded(IConditionNode node)
        {
            if (node is ConditionRule rule)
            {
                return rule.Category == RuleCategory.Quantity;
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

        private async Task<List<string>> RunDeepSearchAsync(List<string> candidateFiles, ConditionGroup rootCondition, List<string> selectedQuantities)
        {
            var matchedFiles = new ConcurrentBag<string>();
            string erxPath = Properties.Settings.Default.ErxPath;
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 4 };

            var quantityRules = GetQuantityRules(rootCondition);

            var allQuantitiesToExport = new HashSet<string>();
            if (selectedQuantities != null)
            {
                foreach (var q in selectedQuantities) allQuantitiesToExport.Add(q);
            }

            foreach (var rule in quantityRules)
            {
                allQuantitiesToExport.Add(rule.TargetProperty);
            }

            if (!allQuantitiesToExport.Any()) return candidateFiles;


            string quantitiesArg = string.Join(";", allQuantitiesToExport);
            string intervalArg = ExtractIntervalFromConditions(rootCondition);

            await Parallel.ForEachAsync(candidateFiles, parallelOptions, async (filePath, cancellationToken) =>
            {
                string tempCsvPath = Path.Combine(Path.GetTempPath(), $"erx_temp_{Guid.NewGuid()}.csv");

                try
                {
                    string arguments = $"source=\"{filePath}\" dest=\"{tempCsvPath}\" quantities=\"{quantitiesArg}\"";

                    if (!string.IsNullOrEmpty(intervalArg))
                    {
                        arguments += $" interval=\"{intervalArg}\"";
                    }

                    var processInfo = new ProcessStartInfo
                    {
                        FileName = erxPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(processInfo))
                    {
                        await process.WaitForExitAsync(cancellationToken);
                    }

                    if (File.Exists(tempCsvPath))
                    {
                        bool conditionMet = AnalyzeCsvFile(tempCsvPath, rootCondition, quantityRules);
                        if (conditionMet)
                        {
                            matchedFiles.Add(filePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Chyba u souboru {filePath}: {ex.Message}");
                }
                finally
                {
                    if (File.Exists(tempCsvPath)) File.Delete(tempCsvPath);
                }
            });

            return matchedFiles.ToList();
        }


        private bool AnalyzeCsvFile(string csvPath, ConditionGroup rootCondition, List<ConditionRule> quantityRules)
        {
            using (var reader = new StreamReader(csvPath))
            {
                string headerLine = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(headerLine)) return false;

                char separator = ';';
                List<string> headers = headerLine.Split(separator).ToList();

                var ruleColumnMap = new Dictionary<ConditionRule, int>();
                foreach (var rule in quantityRules)
                {
                    int colIndex = headers.IndexOf(rule.TargetProperty);
                    if (colIndex == -1) return false;
                    ruleColumnMap[rule] = colIndex;
                }

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] rowValues = line.Split(separator);

                    bool rowMatches = EvaluateRow(rootCondition, rowValues, ruleColumnMap);

                    if (rowMatches)
                    {
                        return true;
                    }
                }
            }
            return false;
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
            // erx.exe čeká formát "startTime-endTime"
            // Projdi strom podmínek (rootNode), najdi v něm pravidla pro Datum Od a Datum Do,
            // a slož je do tohoto stringu. Pokud datum nehledáme, vrať null.

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

    }

}
