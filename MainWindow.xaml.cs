using CeaIndexer.Services;
using CeaIndexer.Views;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace CeaIndexer
{
    public partial class MainWindow : Window
    {
        private bool _isDarkMode = false;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, EventArgs e)
        {
            string erxPath = Properties.Settings.Default.ErxPath;

            if (string.IsNullOrWhiteSpace(erxPath))
            {
                MainContainer.Effect = new BlurEffect { Radius = 15 };

                var firstRunWindow = new FirstRunWindow();
                firstRunWindow.Owner = this;
                firstRunWindow.ShowDialog();

                if (string.IsNullOrWhiteSpace(Properties.Settings.Default.ErxPath))
                {
                    Application.Current.Shutdown();
                }
                else
                {
                    MainContainer.Effect = null;
                }
            }
        }

        // --- Ovládání okna ---
        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                // Maximalizace okna
                this.MaxHeight = SystemParameters.WorkArea.Height + 10;
                this.WindowState = WindowState.Maximized;

                BtnMaximizeIcon.Content = "\xE923"; // Ikonka "dvou čtverečků"
                BtnMaximizeIcon.ToolTip = "Obnovit"; // <--- NOVÝ TEXT
            }
            else
            {
                // Návrat do normální velikosti
                this.WindowState = WindowState.Normal;

                BtnMaximizeIcon.Content = "\xE922"; // Ikonka "jednoho čtverečku"
                BtnMaximizeIcon.ToolTip = "Maximalizovat"; // <--- PŮVODNÍ TEXT
            }
        }

        // --- Navigace ---
        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            // Tady můžeš dát úvodní obrazovku nebo vyčistit Content
            MainContentArea.Content = null;
        }

        private void BtnScanFolder_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new IndexerView();
        }

        private void BtnSearchEngine_Click(object sender, RoutedEventArgs e)
        {
            // Nastaví obsah hlavní plochy na náš nový Tvůrce podmínek
            // (Ujisti se, že máš nahoře using CeaIndexer.Views;)
            MainContentArea.Content = new ConditionBuilderView();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new SettingsView();
        }

        // --- Tmavý/Světlý režim ---
        private void BtnThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            _isDarkMode = !_isDarkMode;

            if (_isDarkMode)
            {
                this.Resources["WindowBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                this.Resources["SidebarBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#151515"));
                this.Resources["HoverBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
                this.Resources["TextBrush"] = Brushes.White;
                IconTheme.Text = "\xE706"; // Sun icon
                IconTheme.Foreground = Brushes.Gold;
            }
            else
            {
                this.Resources["WindowBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ECF0F1"));
                this.Resources["SidebarBgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50"));
                this.Resources["HoverBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34495E"));
                this.Resources["TextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50"));
                IconTheme.Text = "\xE708"; // Moon icon
                IconTheme.Foreground = (SolidColorBrush)this.Resources["TextBrush"];
            }
        }
    }
}