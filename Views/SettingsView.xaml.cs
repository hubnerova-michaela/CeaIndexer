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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CeaIndexer.Views
{

    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {

            TxtErxPath.Text = Properties.Settings.Default.ErxPath;


            string currentLang = Properties.Settings.Default.AppLanguage;
            if (currentLang == "en-US") CmbLanguage.SelectedIndex = 1;
            else CmbLanguage.SelectedIndex = 0;


            var folders = Properties.Settings.Default.DefaultDataFolders;
            if (folders != null)
            {
                foreach (string folder in folders)
                {
                    LstFolders.Items.Add(folder);
                }
            }
        }

        private void BtnBrowseErx_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Vyberte soubor erx.exe",
                Filter = "Spustitelný soubor (erx.exe)|erx.exe|Všechny soubory (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                TxtErxPath.Text = dialog.FileName;
            }
        }

        private void BtnAddFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Vyberte složku s .cea daty"
            };

            if (dialog.ShowDialog() == true)
            {
                if (!LstFolders.Items.Contains(dialog.FolderName))
                {
                    LstFolders.Items.Add(dialog.FolderName);
                }
            }
        }

        private void BtnRemoveFolder_Click(object sender, RoutedEventArgs e)
        {
            if (LstFolders.SelectedItem != null)
            {
                LstFolders.Items.Remove(LstFolders.SelectedItem);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {

            Properties.Settings.Default.ErxPath = TxtErxPath.Text;

            if (CmbLanguage.SelectedItem is ComboBoxItem selectedItem)
            {
                Properties.Settings.Default.AppLanguage = selectedItem.Tag.ToString();
            }


            if (Properties.Settings.Default.DefaultDataFolders == null)
            {
                Properties.Settings.Default.DefaultDataFolders = new System.Collections.Specialized.StringCollection();
            }

            Properties.Settings.Default.DefaultDataFolders.Clear();
            foreach (string folder in LstFolders.Items)
            {
                Properties.Settings.Default.DefaultDataFolders.Add(folder);
            }


            Properties.Settings.Default.Save();

            MessageBox.Show("Nastavení bylo úspěšně uloženo!", "Uloženo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
