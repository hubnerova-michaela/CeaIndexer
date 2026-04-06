using CeaIndexer.Models;
using CeaIndexer.Services;
using CeaIndexer.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CeaIndexer.Views
{

    public partial class IndexerView : UserControl
    {
        public static bool IsScanning { get; private set; } = false;

        private ObservableCollection<FileNode> _rootFolders = new ObservableCollection<FileNode>();
        public IndexerView()
        {
            InitializeComponent();
            TvFolders.ItemsSource = _rootFolders;

            this.Loaded += IndexerView_Loaded;
        }

        private void IndexerView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDataTree();
        }

        private void LoadDataTree()
        {
            _rootFolders.Clear();
            var savedFolders = Properties.Settings.Default.DefaultDataFolders;

            if (savedFolders == null || savedFolders.Count == 0) return;

            foreach (string folderPath in savedFolders)
            {
                if (Directory.Exists(folderPath))
                {
                    var rootNode = CreateDirectoryNode(folderPath);
                    if (rootNode != null)
                    {
                        _rootFolders.Add(rootNode);
                    }
                }
            }
        }

        private FileNode CreateDirectoryNode(string dirPath)
        {
            var dirInfo = new DirectoryInfo(dirPath);
            var node = new FileNode
            {
                Name = dirInfo.Name,
                FullPath = dirInfo.FullName,
                IsFile = false,
                IsChecked = true
            };

            try
            {
                foreach (var subDir in dirInfo.GetDirectories())
                {
                    var subNode = CreateDirectoryNode(subDir.FullName);
                    if (subNode != null)
                    {
                        node.Children.Add(subNode);
                    }
                }

                foreach (var file in dirInfo.GetFiles("*.cea"))
                {
                    node.Children.Add(new FileNode
                    {
                        Name = file.Name,
                        FullPath = file.FullName,
                        IsFile = true,
                        IsChecked = true
                    });
                }
            }
            catch (UnauthorizedAccessException)
            {
                // ignorovat složky bez přístupu
            }

            if (node.Children.Count == 0) return null;

            return node;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        { 
            MessageBox.Show(Properties.Resources.IndexerView_BackMessage, Properties.Resources.IndexerView_BackTitle);
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var node in _rootFolders) node.IsChecked = true;
        }

        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var node in _rootFolders) node.IsChecked = false;
        }

        private async void BtnStartIndex_Click(object sender, RoutedEventArgs e)
        {
            List<string> filesToProcess = GetCheckedFilePaths(_rootFolders);

            if (filesToProcess.Count == 0)
            {
                MessageBox.Show(Properties.Resources.IndexerView_NoFilesSelected, Properties.Resources.IndexerView_ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }


            var progressWindow = new ProgressWindow();

            foreach (var path in filesToProcess)
            {
                progressWindow.FileItems.Add(new ProgressWindow.FileProgressItem { FileName = System.IO.Path.GetFileName(path), FullPath = path });
            }

            progressWindow.GlobalProgressBar.Maximum = filesToProcess.Count;
            progressWindow.TxtGlobalStatus.Text = string.Format(Properties.Resources.IndexerView_ProcessingZero, filesToProcess.Count);

            progressWindow.Show();

            var progressIndicator = new Progress<ScanProgressReport>(report =>
            {
                var item = progressWindow.FileItems.FirstOrDefault(x => x.FullPath == report.FilePath);
                if (item == null) return;

                if (report.Type == ScanProgressReport.ReportType.FileStarted)
                {
                    item.StatusIcon = "🔄";
                    item.FileStats = Properties.Resources.IndexerView_SearchingMeasurePoints;

                    progressWindow.LstFiles.SelectedItem = item;
                }
                else if (report.Type == ScanProgressReport.ReportType.MeasurePointFound)
                {
                    item.FoundPoints.Add(new ProgressWindow.MeasurePointProgressItem
                    {
                        Name = report.ParsedMeasurePoint.Name,
                        DeviceType = report.ParsedMeasurePoint.DeviceType,
                        DetailsSummary = string.Format(Properties.Resources.IndexerView_QuantitiesAndArchivesInfo, report.ParsedMeasurePoint.TotalQuantitiesCount, report.ParsedMeasurePoint.Archives.Count)
                    });
                    item.FileStats = string.Format(Properties.Resources.IndexerView_FoundDevicesInfo, item.FoundPoints.Count);

                    if (progressWindow.LstFiles.SelectedItem == item)
                        progressWindow.CurrentFileStats = item.FileStats;
                }
                else if (report.Type == ScanProgressReport.ReportType.FileCompleted || report.Type == ScanProgressReport.ReportType.FileError)
                {
                    item.StatusIcon = report.Type == ScanProgressReport.ReportType.FileCompleted ? "✅" : "❌";
                    if (report.Type == ScanProgressReport.ReportType.FileError) item.FileStats = Properties.Resources.IndexerView_FileErrorPrefix + report.ErrorMessage;

                    if (progressWindow.LstFiles.SelectedItem == item)
                        progressWindow.CurrentFileStats = item.FileStats;

                    progressWindow.GlobalProgressBar.Value = report.ProcessedFiles;
                    int percentage = (int)((report.ProcessedFiles / (double)report.TotalFiles) * 100);
                    progressWindow.TxtGlobalStatus.Text = string.Format(Properties.Resources.IndexerView_ProcessingProgress, report.ProcessedFiles, report.TotalFiles, percentage);
                }
            });

            try
            {
                BtnStartIndex.IsEnabled = false;
                IsScanning = true;
                progressWindow.IsWorking = true;
                var scanner = new ScannerService();
                List<FileEntry> nactenaData = await scanner.ProcessFilesAsync(filesToProcess, progressIndicator);


                progressWindow.TxtGlobalStatus.Text = string.Format(Properties.Resources.IndexerView_CompleteMessage, nactenaData.Sum(x => x.MeasurePoints.Count), filesToProcess.Count);
                progressWindow.GlobalProgressBar.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DodgerBlue);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Properties.Resources.IndexerView_FatalScannerErrorPrefix, ex.Message), Properties.Resources.IndexerView_ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnStartIndex.IsEnabled = true;
                IsScanning = false;
                if (progressWindow != null)
                {
                    progressWindow.IsWorking = false;
                }
            }

        }

        
        private List<string> GetCheckedFilePaths(System.Collections.ObjectModel.ObservableCollection<ViewModels.FileNode> nodes)
        {
            List<string> paths = new List<string>();
            foreach (var node in nodes)
            {
                if (node.IsFile)
                {
                    if (node.IsChecked)
                    {
                        
                        paths.Add(node.FullPath);
                    }
                }

                else
                {
                    paths.AddRange(GetCheckedFilePaths(node.Children));
                }
                
            }
            return paths;
        }
    }
}
