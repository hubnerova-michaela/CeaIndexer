using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using WinFormsDialogResult = System.Windows.Forms.DialogResult;
using WinFormsFolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace CeaIndexer
{
    public partial class MainWindow : Window
    {
        private AppSettings _settings;
        private bool _isLoadingSettings = false;

        private readonly string[] Watchlist = new[]
        {
            "main_Inputs/Temperature_Ti",
            "main_Inputs/Temperature_Te"
        };

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            InitializeDatabase();
            InitializeLocalization();
        }

        private void LoadSettings()
        {
            _settings = SettingsManager.Load();
            _isLoadingSettings = true;
            ErxPathTextBox.Text = _settings.ErxExePath;
            FolderTextBox.Text = _settings.CeaFolderPath;
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
            TestCsvButton.Content = LocalizationManager.GetString("TestCsv");
            IndexedFilesGroup.Header = LocalizationManager.GetString("IndexedFiles");
            FileDetailsGroup.Header = LocalizationManager.GetString("FileDetails");
            ArchiveColumn.Header = LocalizationManager.GetString("Archive");
            QuantityNameColumn.Header = LocalizationManager.GetString("QuantityName");
            StatusTextBlock.Text = LocalizationManager.GetString("Ready");
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
                MessageBox.Show($"Error initializing database: {ex.Message}", "Initialization Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                var quantityName = SearchTextBox.Text.Trim();
                var op = (OperatorComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                var valueStr = ValueTextBox.Text.Trim();

                using (var db = new AppDbContext())
                {
                    var query = db.Files.Include(f => f.Quantities).AsQueryable();

                    if (double.TryParse(valueStr, out double threshold) && op != "---")
                    {
                        query = query.Where(f => f.Quantities.Any(q =>
                            q.Name.Contains(quantityName) && (
                                (op == ">" && q.MaxValue > threshold) ||
                                (op == "<" && q.MinValue < threshold) ||
                                (op == "=" && Math.Abs((q.AverageValue ?? 0) - threshold) < 0.1)
                            )
                        ));
                    }
                    else if (!string.IsNullOrWhiteSpace(quantityName))
                    {
                        query = query.Where(f =>
                            f.SerialNumber.Contains(quantityName) ||
                            f.RecordName.Contains(quantityName) ||
                            f.Quantities.Any(q => q.Name.Contains(quantityName))
                        );
                    }

                    var results = query.ToList();
                    DatabaseDataGrid.ItemsSource = results;
                    StatusTextBlock.Text = LocalizationManager.GetString("Found", results.Count);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{LocalizationManager.GetString("SearchError")} {ex.Message}",
                    LocalizationManager.GetString("Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                StatusTextBlock.Text = LocalizationManager.GetString("IndexingFiles");
                var files = Directory.GetFiles(folderPath, "*.cea");
                int addedCount = 0;

                using (var db = new AppDbContext())
                {
                    foreach (var filePath in files)
                    {
                        var fileInfo = new FileInfo(filePath);

                        bool exists = await db.Files.AnyAsync(f => f.Path == filePath);
                        if (exists)
                            continue;

                        var argsIdentify = $"source=\"{filePath}\" -identify";
                        var resultIdentify = await Task.Run(() => RunProcess(exePath, argsIdentify));

                        var entry = new FileEntry
                        {
                            Path = filePath,
                            SizeBytes = fileInfo.Length,
                            IndexedAt = DateTime.Now
                        };

                        if (resultIdentify.ExitCode == 0 || resultIdentify.ExitCode == 3)
                            ErxParser.ParseIdentifyOutput(resultIdentify.StdOut, entry);

                        var argsQuantities = $"source=\"{filePath}\" -list-quantities";
                        var resultQuantities = await Task.Run(() => RunProcess(exePath, argsQuantities));

                        if (resultQuantities.ExitCode == 0 || resultQuantities.ExitCode == 3)
                            ErxParser.ParseQuantitiesOutput(resultQuantities.StdOut, entry);

                        foreach (var q in entry.Quantities)
                        {
                            if (Watchlist.Contains(q.Name))
                            {
                                
                                string safeQuantityName = q.Name.Replace("/", "_").Replace("\\", "_");
                                string tempCsvPath = Path.Combine(folderPath, $"temp_{Path.GetFileNameWithoutExtension(filePath)}_{safeQuantityName}.csv");

         
                                var exportArgs = $"source=\"{filePath}\" quantities=\"{q.Name}\" dest=\"{tempCsvPath}\"";
                                await Task.Run(() => RunProcess(exePath, exportArgs));

                             
                                if (File.Exists(tempCsvPath))
                                {
                                    try
                                    {
                                        var stats = CsvAnalyzer.ExtractStatisticsFromCsv(tempCsvPath);
                                        q.MinValue = stats.MinValue;
                                        q.MaxValue = stats.MaxValue;
                                        q.AverageValue = stats.AverageValue;
                                    }
                                    finally
                                    {
                                     
                                        try
                                        {
                                            File.Delete(tempCsvPath);
                                        }
                                        catch
                                        {
                  
                                        }
                                    }
                                }
                            }
                        }

                        db.Files.Add(entry);
                        addedCount++;

                        StatusTextBlock.Text = LocalizationManager.GetString("Processing", addedCount, files.Length);
                    }

                    if (addedCount > 0)
                        await db.SaveChangesAsync();
                }

                StatusTextBlock.Text = LocalizationManager.GetString("NewlyIndexed", addedCount, files.Length);
                await RefreshDbAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{LocalizationManager.GetString("ErrorLoadingFiles")} {ex.Message}",
                    LocalizationManager.GetString("Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = LocalizationManager.GetString("Error");
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

        private async void Help_Click(object sender, RoutedEventArgs e)
        {
            await RunErxAsync("-?", null);
        }

        private async void TestCsv_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = LocalizationManager.GetString("CsvFiles"),
                Title = LocalizationManager.GetString("SelectCsvFile")
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            try
            {
                var result = CsvAnalyzer.AnalyzeValues(openFileDialog.FileName);

                string message = $"{LocalizationManager.GetString("AnalysisSuccessful")}\n\n" +
                                $"{LocalizationManager.GetString("Minimum", result.Min, result.MinTime)}\n" +
                                $"{LocalizationManager.GetString("Maximum", result.Max, result.MaxTime)}";

                MessageBox.Show(message, LocalizationManager.GetString("CsvAnalysisResult"),
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{LocalizationManager.GetString("AnalysisErrorTitle")}: {ex.Message}",
                    LocalizationManager.GetString("Error"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
}
