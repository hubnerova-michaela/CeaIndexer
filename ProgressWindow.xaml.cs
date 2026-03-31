using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;



namespace CeaIndexer
{

    public partial class ProgressWindow : Window, INotifyPropertyChanged
    {

        // Tyhle kolekce se budou automaticky ukazovat v těch XAML ListBoxech
        public ObservableCollection<FileProgressItem> FileItems { get; set; } = new ObservableCollection<FileProgressItem>();

        private ObservableCollection<MeasurePointProgressItem> _currentMeasurePoints;

        public ObservableCollection<MeasurePointProgressItem> CurrentMeasurePoints
        {
            get => _currentMeasurePoints;
            set { _currentMeasurePoints = value; OnPropertyChanged(); }
        }

        private string _currentFileName = "Vyberte soubor ze seznamu vlevo...";
        public string CurrentFileName
        {
            get => _currentFileName;
            set { _currentFileName = value; OnPropertyChanged(); }
        }

        private string _currentFileStats = "";
        public string CurrentFileStats
        {
            get => _currentFileStats;
            set { _currentFileStats = value; OnPropertyChanged(); }
        }
        public ProgressWindow()
        {
            InitializeComponent();
            DataContext = this; // Tímhle oknu řekneme, ať hledá data přímo v této třídě
        }

        private void LstFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstFiles.SelectedItem is FileProgressItem selectedFile)
            {
                CurrentFileName = selectedFile.FileName;
                CurrentFileStats = selectedFile.FileStats;
                CurrentMeasurePoints = selectedFile.FoundPoints; // Kouzlo! Zobrazíme jen přístroje pro tento soubor
            }
        }

        // Nutné pro automatické překreslování UI
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // --- Pomocné třídy pro položky v seznamech ---

        public class FileProgressItem : INotifyPropertyChanged
        {
            public string FileName { get; set; }
            public string FullPath { get; set; }

            private string _statusIcon = "⏳"; // Výchozí: Čeká
            public string StatusIcon
            {
                get => _statusIcon;
                set { _statusIcon = value; OnPropertyChanged(); }
            }

            public ObservableCollection<MeasurePointProgressItem> FoundPoints { get; set; } = new ObservableCollection<MeasurePointProgressItem>();

            private string _fileStats = "Čeká na zpracování...";
            public string FileStats
            {
                get => _fileStats;
                set { _fileStats = value; OnPropertyChanged(); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public class MeasurePointProgressItem : INotifyPropertyChanged
        {
            public string Name { get; set; }
            public string DeviceType { get; set; }

            private string _statusIcon = "✅"; // Přístroj se ukáže většinou až když je nalezen
            public string StatusIcon
            {
                get => _statusIcon;
                set { _statusIcon = value; OnPropertyChanged(); }
            }

            public string DetailsSummary { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { DragMove(); }
        private void BtnMinimize_Click(object sender, RoutedEventArgs e) { WindowState = WindowState.Minimized; }
        private void BtnClose_Click(object sender, RoutedEventArgs e) { Close(); }
    }
}
