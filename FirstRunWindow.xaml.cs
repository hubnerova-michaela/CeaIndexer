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
using System.Windows.Shapes;

namespace CeaIndexer
{

    public partial class FirstRunWindow : Window
    {
        public FirstRunWindow()
        {
            InitializeComponent();

            string systemLang = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
            if (systemLang != "cs")
            {
                CmbLanguage.SelectedIndex = 1;
            }


            if (Properties.Settings.Default.DefaultDataFolders == null)
            {
                Properties.Settings.Default.DefaultDataFolders = new System.Collections.Specialized.StringCollection();
            }
            else
            {

                foreach (string folder in Properties.Settings.Default.DefaultDataFolders)
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
                Title = "Vyberte složku s daty"
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
            
            if (string.IsNullOrWhiteSpace(TxtErxPath.Text))
            {
                MessageBox.Show("Prosím, vyberte cestu k programu erx.exe, abychom mohli pokračovat.",
                                "Chybí cesta",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            
            Properties.Settings.Default.ErxPath = TxtErxPath.Text;

            
            if (CmbLanguage.SelectedItem is ComboBoxItem selectedItem)
            {
                Properties.Settings.Default.AppLanguage = selectedItem.Tag.ToString();
            }

            
            Properties.Settings.Default.Save();

            this.Close();
        }

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { DragMove(); }
        private void BtnClose_Click(object sender, RoutedEventArgs e) { Close(); }
    }
}
