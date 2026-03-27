using CeaIndexer.Services;
using CeaIndexer.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CeaIndexer
{

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, EventArgs e)
        {

            // Podíváme se do nastavení, jestli už známe cestu k erx.exe
            string erxPath = Properties.Settings.Default.ErxPath;

            if (string.IsNullOrWhiteSpace(erxPath))
            {
                // 1. Rozmažeme pozadí
                MainContainer.Effect = new BlurEffect { Radius = 15 };

                // 2.Vytvoříme a vycentrujeme okno
                var firstRunWindow = new FirstRunWindow();
                firstRunWindow.Owner = this;

                // ShowDialog teď už nic nerozbije a nevytvoří duchy
                firstRunWindow.ShowDialog();

                // 3. Kontrola po zavření
                if (string.IsNullOrWhiteSpace(Properties.Settings.Default.ErxPath))
                {
                    Application.Current.Shutdown();
                }
                else
                {
                    MainContainer.Effect = null; // Vypneme rozmazání
                }
            }

        }


        //private async void BtnScanFile_Click(object sender, RoutedEventArgs e)
        //{
        //    OpenFileDialog openFileDialog = new OpenFileDialog();
        //    openFileDialog.Multiselect = true;
        //    openFileDialog.Filter = Properties.Resources.Dialog_Filter_Cea;
        //    openFileDialog.Title = Properties.Resources.Dialog_Title_SelectFiles;

        //    if (openFileDialog.ShowDialog() == true)
        //    {
        //        try
        //        {
        //            this.Cursor = System.Windows.Input.Cursors.Wait;
        //            var scanner = new ScannerService();

        //            foreach (string vybranySoubor in openFileDialog.FileNames)
        //            {
        //                await scanner.ProcessFileAsync(vybranySoubor);
        //            }

        //            MessageBox.Show(
        //                Properties.Resources.Msg_ScanSuccess,
        //                Properties.Resources.Msg_Done,
        //                MessageBoxButton.OK,
        //                MessageBoxImage.Information);
        //        }
        //        catch (InvalidOperationException ex)
        //        {
        //            MessageBox.Show(ex.Message, Properties.Resources.Msg_MissingSettingsTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
        //        }
        //        catch (Exception ex)
        //        {
        //            string errorMessage = $"{Properties.Resources.Msg_ScanError}\n\n{ex.Message}";
        //            MessageBox.Show(errorMessage, Properties.Resources.Msg_ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        //        }
        //        finally
        //        {
        //            this.Cursor = System.Windows.Input.Cursors.Arrow;
        //        }

        //    }
        //}

        //private async void BtnScanFolder_Click(object sender, RoutedEventArgs e)
        //{
        //    var dialog = new Microsoft.Win32.OpenFolderDialog
        //    {
        //        Title = Properties.Resources.Dialog_Title_SelectFolder,
        //        Multiselect = false // Chceme jednu hlavní složku
        //    };

        //    if (dialog.ShowDialog() == true)
        //    {
        //        try
        //        {
        //            this.Cursor = System.Windows.Input.Cursors.Wait;
        //            var scanner = new ScannerService();

        //            await scanner.ProcessDirectoryAsync(dialog.FolderName);

        //            MessageBox.Show(Properties.Resources.Msg_ScanSuccess, Properties.Resources.Msg_Done, MessageBoxButton.OK, MessageBoxImage.Information);
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show($"{Properties.Resources.Msg_ScanError}\n\n{ex.Message}", Properties.Resources.Msg_ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        //        }
        //        finally
        //        {
        //            this.Cursor = System.Windows.Input.Cursors.Arrow;
        //        }
        //    }
        //}

        private void NavigateToIndexerView()
        {
            MainContentArea.Content = new IndexerView();
        }


        private void BtnScanFolder_Click(object sender, RoutedEventArgs e)
        {
            NavigateToIndexerView();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new SettingsView();
        }
    }
}
