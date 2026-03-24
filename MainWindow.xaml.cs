using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using MessageBox = System.Windows.MessageBox;
using WinFormsDialogResult = System.Windows.Forms.DialogResult;
using WinFormsFolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace CeaIndexer
{
    public partial class MainWindow : Window
    {
        private AppSettings _settings;
        private bool _isLoadingSettings = false;

        private string[] _watchlist = new[] { "main_Inputs/Temperature_Ti", "main_Inputs/Temperature_Te" };

        public string[] Watchlist
        {
            get => _watchlist;
            set
            {
                if (value == null || value.Length == 0)
                {
                    Logger.Log("WARNING: Watchlist is empty - no quantities will be analyzed");
                    _watchlist = value ?? Array.Empty<string>();
                    return;
                }

                if (value.Any(q => string.IsNullOrWhiteSpace(q)))
                {
                    Logger.Log("WARNING: Watchlist contains empty entries");
                }

                _watchlist = value;
                Logger.Log($"Watchlist updated: {_watchlist.Length} quantities");
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            
            // Set Czech language text with diacritics in combo box
            if (LanguageComboBox.Items.Count >= 2 && LanguageComboBox.Items[1] is ComboBoxItem czechItem)
            {
                czechItem.Content = "Èeština";
            }
            
            LoadSettings();
            InitializeDatabase();
            InitializeLocalization();
            PopulateSearchComboBox();
        }

        private void LoadSettings()
        {
            _settings = SettingsManager.Load();
            _isLoadingSettings = true;
            ErxPathTextBox.Text = _settings.ErxExePath;
            FolderTextBox.Text = _settings.CeaFolderPath;
            
            if (!string.IsNullOrEmpty(_settings.WatchedQuantities))
            {
                Watchlist = _settings.WatchedQuantities.Split(';');
            }
            
            _isLoadingSettings = false;

            var langItem = LanguageComboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Tag.ToString() == _settings.Language);
            if (langItem != null)
                LanguageComboBox.SelectedItem = langItem;
        }

        private void SaveSettings()
        {
            if (_isLoadingSettings)
                return;

            _settings.ErxExePath = ErxPathTextBox.Text;
            _settings.CeaFolderPath = FolderTextBox.Text;
            SettingsManager.Save(_settings);
        }

        private void InitializeLocalization()
        {
            LocalizationManager.SetLanguage(_settings.Language);
            UpdateUIText();
        }

        private void UpdateUIText()
        {
            LanguageLabel.Text = LocalizationManager.GetString("Language");
            ErxPathLabel.Text = LocalizationManager.GetString("PathToErxExe");
            BrowseErxButton.Content = LocalizationManager.GetString("Browse");
            CeaFolderLabel.Text = LocalizationManager.GetString("CeaFolder");
            BrowseFolderButton.Content = LocalizationManager.GetString("Browse");
            LoadFilesButton.Content = LocalizationManager.GetString("LoadFiles");
            RefreshButton.Content = LocalizationManager.GetString("Refresh");
            QuantityLabel.Text = LocalizationManager.GetString("Quantity");
            OperatorLabel.Text = LocalizationManager.GetString("Operator");
            FilterButton.Content = LocalizationManager.GetString("Filter");
            WatchlistButton.Content = LocalizationManager.GetString("ManageWatchlist");
            IndexedFilesGroup.Header = LocalizationManager.GetString("IndexedFiles");
            FileDetailsGroup.Header = LocalizationManager.GetString("FileDetails");
            ArchiveColumn.Header = LocalizationManager.GetString("Archive");
            QuantityNameColumn.Header = LocalizationManager.GetString("QuantityName");
            StatusTextBlock.Text = LocalizationManager.GetString("Ready");
            HideLoadingIndicators();
            CurrentFileStatusTextBlock.Text = "File: -";
            LastFileResultTextBlock.Text = "Last result: -";
            PhaseProgressBar.Value = 0;
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem item)
            {
                string language = item.Tag.ToString();
                LocalizationManager.SetLanguage(language);
                _settings.Language = language;
                SettingsManager.Save(_settings);
                UpdateUIText();
            }
        }

        private void ErxPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveSettings();
        }

        private void FolderTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveSettings();
        }

        private void InitializeDatabase()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    db.Database.EnsureCreated();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LocalizationManager.GetString("InitializationError")}: {ex.Message}",
                    LocalizationManager.GetString("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BrowseErx_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executable (*.exe)|*.exe",
                Title = LocalizationManager.GetString("PathToErxExe")
            };

            if (dlg.ShowDialog() == true)
                ErxPathTextBox.Text = dlg.FileName;
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new WinFormsFolderBrowserDialog();
            if (dlg.ShowDialog() == WinFormsDialogResult.OK)
                FolderTextBox.Text = dlg.SelectedPath;
        }

        private void SearchDb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
              
                var quantityName = SearchComboBox.Text.Trim();
                var op = (OperatorComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                var valueStr = ValueTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(quantityName) && string.IsNullOrWhiteSpace(valueStr))
                {
                    StatusTextBlock.Text = LocalizationManager.GetString("PleaseEnterSearchCriteria");
                    DatabaseDataGrid.ItemsSource = null;
                    return;
                }

                using (var db = new AppDbContext())
                {
                    var query = db.Files.Include(f => f.Quantities).AsQueryable();

                    if (double.TryParse(valueStr, out double threshold) && op != "---")
                    {
                        query = query.Where(f => f.Quantities.Any(q =>
                            !string.IsNullOrEmpty(q.Name) &&
                            q.Name.Contains(quantityName) && (
                                (op == ">" && q.MaxValue.HasValue && q.MaxValue > threshold) ||
                                (op == "<" && q.MinValue.HasValue && q.MinValue < threshold) ||
                                (op == "=" && q.AverageValue.HasValue && Math.Abs(q.AverageValue.Value - threshold) < 0.1)
                            )
                        ));
                    }
                    else if (!string.IsNullOrWhiteSpace(quantityName))
                    {
                        query = query.Where(f =>
                            (!string.IsNullOrEmpty(f.SerialNumber) && f.SerialNumber.Contains(quantityName)) ||
                            (!string.IsNullOrEmpty(f.RecordName) && f.RecordName.Contains(quantityName)) ||
                            f.Quantities.Any(q => !string.IsNullOrEmpty(q.Name) && q.Name.Contains(quantityName))
                        );
                    }

                    var results = query.ToList();
                    DatabaseDataGrid.ItemsSource = results;
                    StatusTextBlock.Text = LocalizationManager.GetString("Found", results.Count);
                    Logger.Log($"Search executed: {results.Count} results");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{LocalizationManager.GetString("SearchError")} {ex.Message}",
                    LocalizationManager.GetString("Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.LogError("SearchDb_Click error", ex);
            }
        }

        private async void LoadFiles_Click(object sender, RoutedEventArgs e)
        {
            var folderPath = FolderTextBox.Text;
            var exePath = ErxPathTextBox.Text;

            if (!Directory.Exists(folderPath))
            {
                MessageBox.Show(LocalizationManager.GetString("InvalidFolderPath"),
                    LocalizationManager.GetString("ValidationError"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                MessageBox.Show(LocalizationManager.GetString("InvalidErxPath"),
                    LocalizationManager.GetString("ValidationError"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ShowLoadingIndicators();

                StatusTextBlock.Text = LocalizationManager.GetString("Phase1ScanningMetadata");
                await Phase1_ScanMetadataAsync(folderPath, exePath);

                HideLoadingIndicators();

                // show files in indexed files after phase 1
                await RefreshDbAsync();
                PopulateSearchComboBox();

                StatusTextBlock.Text = LocalizationManager.GetString("Phase2WaitingSelection");
                bool proceedToPhase3 = await Phase2_InteractiveWatchlistSelectionAsync();

                if (proceedToPhase3)
                {
                    ShowLoadingIndicators();
                    StatusTextBlock.Text = LocalizationManager.GetString("Phase3DeepAnalysis");
                    await Phase3_DeepAnalysisAsync(folderPath, exePath);
                    HideLoadingIndicators();
                    StatusTextBlock.Text = LocalizationManager.GetString("Complete");
                }
                else
                {
                    StatusTextBlock.Text = LocalizationManager.GetString("CancelledByUser");
                }

                await RefreshDbAsync();
                PopulateSearchComboBox();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{LocalizationManager.GetString("ErrorLoadingFiles")} {ex.Message}",
                    LocalizationManager.GetString("Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = LocalizationManager.GetString("Error");
                Logger.LogError("LoadFiles_Click error", ex);
            }
            finally
            {
                HideLoadingIndicators();
            }
        }

        private void PopulateSearchComboBox()
        {
            try
            {
                // load current watchlist and fill SearchComboBox
                var watchlist = WatchlistManager.LoadWatchlist();
                
                
                SearchComboBox.Items.Clear();

                // add watchlist quantities as dropdown items
                foreach (var quantity in watchlist)
                {
                    SearchComboBox.Items.Add(quantity);
                }

                Logger.Log($"Search combo box populated with {watchlist.Length} quantities");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error populating search combo box", ex);
            }
        }

        private async Task Phase1_ScanMetadataAsync(string folderPath, string exePath)
        {
            var files = Directory.GetFiles(folderPath, "*.cea");
            int processedCount = 0;
            int skippedCount = 0;
            int repairedCount = 0;
            int errorCount = 0;
            var duplicatePathsInRun = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            PhaseProgressBar.Minimum = 0;
            PhaseProgressBar.Maximum = files.Length == 0 ? 1 : files.Length;
            PhaseProgressBar.Value = 0;

            using (var db = new AppDbContext())
            {
                for (int i = 0; i < files.Length; i++)
                {
                    var filePath = files[i];
                    string displayName = Path.GetFileName(filePath);
                    CurrentFileStatusTextBlock.Text = $"File: {displayName}";
                    StatusTextBlock.Text = LocalizationManager.GetString("Phase1Processing", i + 1, files.Length);

                    try
                    {
                        bool originalExists = await db.Files.AnyAsync(f => f.Path == filePath);
                        if (originalExists)
                        {
                            skippedCount++;
                            LastFileResultTextBlock.Text = $"Last result: SKIPPED {displayName} (already indexed)";
                            continue;
                        }

                        string currentFilePath = filePath;

                        var identify = await TryIdentifyAsync(exePath, currentFilePath);

                        if (!identify.Success)
                        {
                            Logger.Log($"Identify failed for {displayName}, attempting repair...");

                            string repairedPath = await Phase0_NormalizeCeaFileAsync(currentFilePath, exePath);
                            if (!string.IsNullOrWhiteSpace(repairedPath) && File.Exists(repairedPath))
                            {
                                var repairedIdentify = await TryIdentifyAsync(exePath, repairedPath);
                                if (repairedIdentify.Success)
                                {
                                    currentFilePath = repairedPath;
                                    identify = repairedIdentify;
                                    repairedCount++;
                                    Logger.Log($"File repaired and identify succeeded: {displayName} -> {Path.GetFileName(repairedPath)}");
                                }
                                else
                                {
                                    Logger.Log($"Repair created file but identify still failed: {Path.GetFileName(repairedPath)}. Keeping original path for indexing.");
                                }
                            }
                            else
                            {
                                Logger.Log($"Failed to repair {displayName}");
                            }
                        }

                        if (!duplicatePathsInRun.Add(currentFilePath))
                        {
                            skippedCount++;
                            LastFileResultTextBlock.Text = $"Last result: SKIPPED {displayName} (duplicate path in run)";
                            Logger.Log($"Skipping duplicate path in current scan run: {currentFilePath}");
                            continue;
                        }

                        bool finalPathExists = await db.Files.AnyAsync(f => f.Path == currentFilePath);
                        if (finalPathExists)
                        {
                            skippedCount++;
                            LastFileResultTextBlock.Text = $"Last result: SKIPPED {displayName} (final path already indexed)";
                            continue;
                        }

                        var entry = new FileEntry
                        {
                            Path = currentFilePath,
                            SizeBytes = File.Exists(currentFilePath) ? new FileInfo(currentFilePath).Length : 0,
                            IndexedAt = DateTime.Now,
                            RecordName = Path.GetFileNameWithoutExtension(filePath)
                        };

                        if (identify.Success && !string.IsNullOrWhiteSpace(identify.Output))
                        {
                            ErxParser.ParseIdentifyOutput(identify.Output, entry);
                        }
                        else
                        {
                            Logger.Log($"Identify metadata unavailable for {displayName}. Saving file with fallback metadata.");
                        }

                        var argsQuantities = $"source=\"{currentFilePath}\" -list-quantities";
                        var resultQuantities = await Task.Run(() => RunProcess(exePath, argsQuantities));

                        if (resultQuantities.ExitCode == 0 || resultQuantities.ExitCode == 3)
                            ErxParser.ParseQuantitiesOutput(resultQuantities.StdOut, entry);

                        db.Files.Add(entry);
                        await db.SaveChangesAsync();

                        processedCount++;
                        LastFileResultTextBlock.Text = $"Last result: OK {displayName}";
                    }
                    catch (DbUpdateException ex)
                    {
                        errorCount++;
                        db.ChangeTracker.Clear();
                        LastFileResultTextBlock.Text = $"Last result: ERROR {displayName}";
                        OutputTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] ERROR {displayName}: {ex.GetBaseException().Message}{Environment.NewLine}");
                        OutputTextBox.ScrollToEnd();
                        Logger.LogError($"Phase 1 DB error for file {displayName}", ex);
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        db.ChangeTracker.Clear();
                        LastFileResultTextBlock.Text = $"Last result: ERROR {displayName}";
                        OutputTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] ERROR {displayName}: {ex.Message}{Environment.NewLine}");
                        OutputTextBox.ScrollToEnd();
                        Logger.LogError($"Phase 1 error for file {displayName}", ex);
                    }
                    finally
                    {
                        PhaseProgressBar.Value = i + 1;
                    }
                }

                Logger.Log($"Phase 1 complete: {processedCount} new files scanned, {skippedCount} skipped, {repairedCount} files repaired, {errorCount} errors");

                if (errorCount > 0)
                {
                    MessageBox.Show(
                        $"Phase 1 finished with {errorCount} error(s). Check the output panel for file names.",
                        LocalizationManager.GetString("WarningTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                else if (processedCount == 0 && skippedCount > 0)
                {
                    MessageBox.Show(
                        LocalizationManager.GetString("AllFilesAlreadyIndexed", skippedCount),
                        LocalizationManager.GetString("InfoTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        private async Task<(bool Success, string Output)> TryIdentifyAsync(string exePath, string filePath)
        {
            var argsIdentify = $"source=\"{filePath}\" -identify";
            var resultIdentify = await Task.Run(() => RunProcess(exePath, argsIdentify));

            var output = !string.IsNullOrWhiteSpace(resultIdentify.StdOut)
                ? resultIdentify.StdOut
                : resultIdentify.StdErr;

            bool exitCodeOk = resultIdentify.ExitCode == 0 || resultIdentify.ExitCode == 3;
            bool hasParsableOutput = !string.IsNullOrWhiteSpace(output) && output.Contains(':');

            if (!exitCodeOk || !hasParsableOutput)
            {
                var preview = string.IsNullOrWhiteSpace(output)
                    ? "<empty output>"
                    : output.Length > 300 ? output[..300] + "..." : output;

                Logger.Log($"Identify not parsable for '{Path.GetFileName(filePath)}' (exit={resultIdentify.ExitCode}). Output preview: {preview}");
            }

            return (exitCodeOk && hasParsableOutput, output ?? string.Empty);
        }

        /// <summary>
        /// PHASE 0:
        /// </summary>
        private async Task<string> Phase0_NormalizeCeaFileAsync(string sourcePath, string exePath)
        {
            try
            {
                // vytvorit nazev pro opraveny soubor (puv_repaired.cea)
                string directory = Path.GetDirectoryName(sourcePath);
                string fileName = Path.GetFileNameWithoutExtension(sourcePath);
                string repairedPath = Path.Combine(directory, $"{fileName}_repaired.cea");

                
                if (File.Exists(repairedPath))
                {
                    try
                    {
                        File.Delete(repairedPath);
                    }
                    catch { }
                }

                
                string arguments = $"source=\"{sourcePath}\" dest=\"{repairedPath}\"";

                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null)
                        return null;

                    
                    bool finished = await Task.Run(() => process.WaitForExit(30000));

                    if (!finished)
                    {
                        try { process.Kill(); } catch { }
                        Logger.Log($"Normalization timeout for {Path.GetFileName(sourcePath)}");
                        return null;
                    }

                    
                    if (File.Exists(repairedPath))
                    {
                        var fileInfo = new FileInfo(repairedPath);
                        if (fileInfo.Length > 0)
                        {
                            Logger.Log($"Successfully normalized: {Path.GetFileName(sourcePath)} -> {Path.GetFileName(repairedPath)}");
                            return repairedPath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error during file normalization: {ex.Message}", ex);
            }

            return null;
        }

        private async Task<bool> Phase2_InteractiveWatchlistSelectionAsync()
        {
            using (var db = new AppDbContext())
            {
                var allQuantityNames = await db.Quantities
                    .Select(q => q.Name)
                    .Distinct()
                    .ToListAsync();

                if (allQuantityNames.Count == 0)
                {
                    MessageBox.Show(
                        LocalizationManager.GetString("NoQuantitiesFoundInScannedFiles"),
                        LocalizationManager.GetString("InfoTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return false;
                }

                var watchlistWindow = new WatchlistWindow();

                var quantityViewModels = new ObservableCollection<QuantityViewModel>();
                var currentWatchlist = WatchlistManager.LoadWatchlist();

                var allRelevantQuantities = allQuantityNames
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                foreach (var name in allRelevantQuantities)
                {
                    quantityViewModels.Add(new QuantityViewModel
                    {
                        Name = name,
                        Archive = name.Contains('_') ? name.Substring(0, name.IndexOf('_')) : LocalizationManager.GetString("UnknownArchive"),
                        IsSelected = currentWatchlist.Contains(name)
                    });
                }

                watchlistWindow.LoadQuantities(quantityViewModels);

                if (watchlistWindow.ShowDialog() == true)
                {
                    var selectedQuantities = watchlistWindow.GetSelectedQuantities();

                    if (selectedQuantities.Length == 0)
                    {
                        MessageBox.Show(
                            LocalizationManager.GetString("NoQuantitiesSelectedDeepAnalysis"),
                            LocalizationManager.GetString("WarningTitle"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        Logger.Log("Phase 2: No quantities selected");
                        return false;
                    }

                    WatchlistManager.SaveWatchlist(selectedQuantities);
                    Watchlist = selectedQuantities;
                    Logger.Log($"Phase 2 complete: {selectedQuantities.Length} quantities selected");
                    return true;
                }

                MessageBox.Show(
                    LocalizationManager.GetString("Phase1ScannedAndPhase3Later", allQuantityNames.Count),
                    LocalizationManager.GetString("SelectionCancelled"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Logger.Log("Phase 2: User cancelled - watchlist selection");
                return false;
            }
        }

        private async Task Phase3_DeepAnalysisAsync(string folderPath, string exePath, IEnumerable<string>? targetQuantities = null)
        {
            var watchlist = WatchlistManager.LoadWatchlist();

            if (watchlist.Length == 0)
            {
                MessageBox.Show(
                    LocalizationManager.GetString("NoQuantitiesSelectedForDeepAnalysis"),
                    LocalizationManager.GetString("InfoTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var effectiveWatchlist = watchlist;
            if (targetQuantities != null)
            {
                var targetSet = new HashSet<string>(targetQuantities, StringComparer.OrdinalIgnoreCase);
                effectiveWatchlist = watchlist
                    .Where(q => targetSet.Contains(q))
                    .ToArray();

                if (effectiveWatchlist.Length == 0)
                {
                    Logger.Log("Phase 3 skipped: no newly added quantities to analyze.");
                    return;
                }
            }

            var activeWatchlistSet = new HashSet<string>(effectiveWatchlist, StringComparer.OrdinalIgnoreCase);

            using (var db = new AppDbContext())
            {
                var filesWithQuantities = await db.Files
                    .Include(f => f.Quantities)
                    .ToListAsync();

                int processedCount = 0;
                int analyzedCount = 0;
                int skippedCount = 0;
                int errorCount = 0;

                PhaseProgressBar.Minimum = 0;
                PhaseProgressBar.Maximum = filesWithQuantities.Count == 0 ? 1 : filesWithQuantities.Count;
                PhaseProgressBar.Value = 0;

                for (int i = 0; i < filesWithQuantities.Count; i++)
                {
                    var fileEntry = filesWithQuantities[i];
                    var displayName = string.IsNullOrWhiteSpace(fileEntry.RecordName)
                        ? Path.GetFileName(fileEntry.Path)
                        : fileEntry.RecordName;

                    CurrentFileStatusTextBlock.Text = $"File: {displayName}";
                    StatusTextBlock.Text = LocalizationManager.GetString("Phase3Processing", i + 1, filesWithQuantities.Count);

                    var quantitiesToAnalyze = fileEntry.Quantities
                        .Where(q => !string.IsNullOrWhiteSpace(q.Name) && activeWatchlistSet.Contains(q.Name))
                        .ToList();

                    if (quantitiesToAnalyze.Count == 0)
                    {
                        skippedCount++;
                        processedCount++;
                        LastFileResultTextBlock.Text = $"Last result: SKIPPED {displayName} (no selected quantities)";
                        PhaseProgressBar.Value = i + 1;
                        continue;
                    }

                    var quantitiesParam = QuantityExtractor.BuildQuantitiesParameter(quantitiesToAnalyze, effectiveWatchlist);

                    if (string.IsNullOrEmpty(quantitiesParam))
                    {
                        skippedCount++;
                        processedCount++;
                        LastFileResultTextBlock.Text = $"Last result: SKIPPED {displayName} (empty quantities param)";
                        PhaseProgressBar.Value = i + 1;
                        continue;
                    }

                    string tempCsvPath = Path.Combine(Path.GetTempPath(), $"cea_export_{Guid.NewGuid()}.csv");

                    try
                    {
                        var exportArgs = $"source=\"{fileEntry.Path}\" quantities=\"{quantitiesParam}\" dest=\"{tempCsvPath}\"";
                        var result = await Task.Run(() => RunProcessWithTimeout(exePath, exportArgs, 120000));

                        if (result.TimedOut)
                        {
                            errorCount++;
                            LastFileResultTextBlock.Text = $"Last result: ERROR {displayName} (timeout)";
                            OutputTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] ERROR {displayName}: export timeout after 120s{Environment.NewLine}");
                            OutputTextBox.ScrollToEnd();
                            Logger.Log($"Phase 3 timeout for {displayName}");
                        }
                        else if ((result.ExitCode == 0 || result.ExitCode == 3) && File.Exists(tempCsvPath))
                        {
                            var stats = CsvAnalyzer.ExtractStatisticsFromCsv(tempCsvPath);

                            QuantityExtractor.AssignStatisticsToQuantities(
                                quantitiesToAnalyze,
                                stats.MinValue,
                                stats.MaxValue,
                                stats.AverageValue);

                            analyzedCount++;
                            LastFileResultTextBlock.Text = $"Last result: OK {displayName}";
                            Logger.Log($"Analyzed {quantitiesToAnalyze.Count} quantities for file {displayName}");
                        }
                        else if (result.ExitCode != 0 && result.ExitCode != 3)
                        {
                            errorCount++;
                            LastFileResultTextBlock.Text = $"Last result: ERROR {displayName} (exit {result.ExitCode})";
                            OutputTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] ERROR {displayName}: exit {result.ExitCode}, {result.StdErr}{Environment.NewLine}");
                            OutputTextBox.ScrollToEnd();
                            Logger.Log($"erx.exe failed for {displayName}: exit code {result.ExitCode}, stderr: {result.StdErr}");
                        }
                        else
                        {
                            errorCount++;
                            LastFileResultTextBlock.Text = $"Last result: ERROR {displayName} (CSV missing)";
                            OutputTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] ERROR {displayName}: CSV was not created{Environment.NewLine}");
                            OutputTextBox.ScrollToEnd();
                            Logger.Log($"CSV not created for {displayName} despite exit code {result.ExitCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        LastFileResultTextBlock.Text = $"Last result: ERROR {displayName}";
                        OutputTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] ERROR {displayName}: {ex.Message}{Environment.NewLine}");
                        OutputTextBox.ScrollToEnd();
                        Logger.LogError($"Error processing file {displayName}", ex);
                    }
                    finally
                    {
                        try
                        {
                            if (File.Exists(tempCsvPath))
                            {
                                for (int retryCount = 0; retryCount < 3; retryCount++)
                                {
                                    try
                                    {
                                        File.Delete(tempCsvPath);
                                        break;
                                    }
                                    catch when (retryCount < 2)
                                    {
                                        System.Threading.Thread.Sleep(100);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Failed to delete temp CSV {tempCsvPath}", ex);
                        }

                        processedCount++;
                        PhaseProgressBar.Value = i + 1;
                    }
                }

                await db.SaveChangesAsync();
                Logger.Log($"Phase 3 complete: {processedCount} processed, {analyzedCount} analyzed, {skippedCount} skipped, {errorCount} errors");
            }
        }

        private static (int ExitCode, string StdOut, string StdErr, bool TimedOut) RunProcessWithTimeout(string exe, string args, int timeoutMs)
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using var process = Process.Start(psi);
                if (process == null)
                    return (-1, "", "Failed to start process", false);

                var stdOutTask = process.StandardOutput.ReadToEndAsync();
                var stdErrTask = process.StandardError.ReadToEndAsync();

                if (!process.WaitForExit(timeoutMs))
                {
                    try { process.Kill(true); } catch { }
                    return (-1, "", $"Timeout after {timeoutMs} ms", true);
                }

                Task.WaitAll(stdOutTask, stdErrTask);
                return (process.ExitCode, stdOutTask.Result, stdErrTask.Result, false);
            }
            catch (Exception ex)
            {
                return (-1, "", $"Exception: {ex.Message}", false);
            }
        }

        private async void RefreshDb_Click(object sender, RoutedEventArgs e)
        {
            await RefreshDbAsync();
        }

        private async Task RefreshDbAsync()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var allRecords = db.Files.ToList();
                    DatabaseDataGrid.ItemsSource = allRecords;
                    PopulateSearchComboBox();
                    StatusTextBlock.Text = LocalizationManager.GetString("DisplayingRecords", allRecords.Count);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{LocalizationManager.GetString("ErrorRefreshingDb")} {ex.Message}",
                    LocalizationManager.GetString("Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DatabaseDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (DatabaseDataGrid.SelectedItem is FileEntry selectedFile)
                {
                    using (var db = new AppDbContext())
                    {
                        var quantities = db.Quantities
                            .Where(q => q.FileEntryId == selectedFile.Id)
                            .OrderBy(q => q.Archive)
                            .ThenBy(q => q.Name)
                            .ToList();

                        QuantitiesDataGrid.ItemsSource = quantities;
                        StatusTextBlock.Text = LocalizationManager.GetString("File", selectedFile.RecordName, quantities.Count);
                    }
                }
                else
                {
                    QuantitiesDataGrid.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{LocalizationManager.GetString("ErrorLoadingQuantities")} {ex.Message}",
                    LocalizationManager.GetString("Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenFileExternalButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is FrameworkElement element && element.DataContext is FileEntry fileEntry)
                {
                    OpenFileExternally(fileEntry);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error opening file: {ex.Message}",
                    LocalizationManager.GetString("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Logger.LogError("OpenFileExternalButton_Click failed", ex);
            }
        }



        private void OpenFileExternally(FileEntry selectedFile)
        {
            if (!File.Exists(selectedFile.Path))
            {
                MessageBox.Show(
                    $"File not found: {selectedFile.Path}",
                    LocalizationManager.GetString("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            bool success = EnvisHelper.OpenInDefaultApplication(selectedFile.Path);

            if (!success)
            {
                MessageBox.Show(
                    "Failed to open file. Make sure the file exists and is accessible.",
                    LocalizationManager.GetString("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void Help_Click(object sender, RoutedEventArgs e)
        {
            await RunErxAsync("-?", null);
        }

        private async Task RunErxAsync(string arguments, FileInfo? sourceFile)
        {
            var exe = ErxPathTextBox.Text;

            if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
            {
                MessageBox.Show(LocalizationManager.GetString("InvalidErxPath"),
                    LocalizationManager.GetString("ValidationError"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                OutputTextBox.Clear();
                StatusTextBlock.Text = LocalizationManager.GetString("Running");

                var result = await Task.Run(() => RunProcess(exe, arguments));

                var header = new StringBuilder();
                header.AppendLine($"> {exe} {arguments}");
                header.AppendLine($"> Exit code: {result.ExitCode}");
                header.AppendLine(new string('-', 80));

                OutputTextBox.Text = header + result.StdOut;

                if (!string.IsNullOrWhiteSpace(result.StdErr))
                {
                    OutputTextBox.AppendText("\nSTDERR:\n");
                    OutputTextBox.AppendText(result.StdErr);
                }

                StatusTextBlock.Text = LocalizationManager.GetString("Complete");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{LocalizationManager.GetString("ErrorRunningErx")} {ex.Message}",
                    LocalizationManager.GetString("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                StatusTextBlock.Text = LocalizationManager.GetString("Error");
            }
        }

        private static (int ExitCode, string StdOut, string StdErr) RunProcess(string exe, string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using var process = Process.Start(psi);
                if (process == null)
                    return (-1, "", "Failed to start process");

                var stdOut = process.StandardOutput.ReadToEnd();
                var stdErr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return (process.ExitCode, stdOut, stdErr);
            }
            catch (Exception ex)
            {
                return (-1, "", $"Exception: {ex.Message}");
            }
        }

        private async void WatchlistButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentWatchlist = WatchlistManager.LoadWatchlist();
                var oldWatchlistCopy = currentWatchlist.ToArray();

                string[]? selectedQuantities = null;

                using (var db = new AppDbContext())
                {
                    var allQuantities = db.Quantities
                        .Select(q => q.Name)
                        .Distinct()
                        .ToList();

                    if (allQuantities.Count == 0)
                    {
                        MessageBox.Show(
                            LocalizationManager.GetString("NoQuantitiesFoundInDatabaseRunPhase1"),
                            LocalizationManager.GetString("InfoTitle"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        return;
                    }

                    var quantityViewModels = new ObservableCollection<QuantityViewModel>();

                    var allRelevant = allQuantities
                        .Distinct()
                        .OrderBy(x => x)
                        .ToList();

                    foreach (var name in allRelevant)
                    {
                        quantityViewModels.Add(new QuantityViewModel
                        {
                            Name = name,
                            Archive = name.Contains('_')
                                ? name.Substring(0, name.IndexOf('_'))
                                : LocalizationManager.GetString("UnknownArchive"),
                            IsSelected = currentWatchlist.Contains(name)
                        });
                    }

                    var watchlistWindow = new WatchlistWindow();
                    watchlistWindow.LoadQuantities(quantityViewModels);

                    if (watchlistWindow.ShowDialog() != true)
                    {
                        Logger.Log("Watchlist editing cancelled");
                        return;
                    }

                    selectedQuantities = watchlistWindow.GetSelectedQuantities();

                    if (selectedQuantities.Length == 0)
                    {
                        MessageBox.Show(
                            LocalizationManager.GetString("NoQuantitiesSelected"),
                            LocalizationManager.GetString("WarningTitle"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    WatchlistManager.SaveWatchlist(selectedQuantities);
                    Watchlist = selectedQuantities;
                    PopulateSearchComboBox();

                    MessageBox.Show(
                        LocalizationManager.GetString("WatchlistUpdatedSuccessfully", selectedQuantities.Length),
                        LocalizationManager.GetString("SuccessTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    Logger.Log($"Watchlist manually updated: {selectedQuantities.Length} quantities");
                }

                if (selectedQuantities == null)
                    return;

                var oldSet = new HashSet<string>(oldWatchlistCopy, StringComparer.OrdinalIgnoreCase);
                var newlyAddedQuantities = selectedQuantities
                    .Where(q => !oldSet.Contains(q))
                    .ToArray();

                if (newlyAddedQuantities.Length == 0)
                    return;

                var exePath = ErxPathTextBox.Text;
                if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                {
                    MessageBox.Show(LocalizationManager.GetString("InvalidErxPath"),
                        LocalizationManager.GetString("ValidationError"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var db = new AppDbContext())
                {
                    if (!await db.Files.AnyAsync())
                    {
                        MessageBox.Show(
                            LocalizationManager.GetString("NoQuantitiesFoundInDatabaseRunPhase1"),
                            LocalizationManager.GetString("InfoTitle"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        return;
                    }
                }

                StatusTextBlock.Text = LocalizationManager.GetString("Phase3DeepAnalysis");
                ShowLoadingIndicators();

                await Phase3_DeepAnalysisAsync(FolderTextBox.Text, exePath, newlyAddedQuantities);
                await RefreshDbAsync();
                StatusTextBlock.Text = LocalizationManager.GetString("Complete");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationManager.GetString("ErrorEditingWatchlist", ex.Message),
                    LocalizationManager.GetString("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Logger.LogError("WatchlistButton_Click failed", ex);
            }
            finally
            {
                HideLoadingIndicators();
            }
        }

        private void ShowLoadingIndicators()
        {
            ActivityStatusItem.Visibility = Visibility.Visible;
        }

        private void HideLoadingIndicators()
        {
            ActivityStatusItem.Visibility = Visibility.Collapsed;
            PhaseProgressBar.Value = 0;
            CurrentFileStatusTextBlock.Text = "File: -";
            LastFileResultTextBlock.Text = "Last result: -";
        }
    }
}
