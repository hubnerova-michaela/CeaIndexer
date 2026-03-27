using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CeaIndexer.ViewModels
{
    public class FileNode : INotifyPropertyChanged
    {
        public string Name { get; set; } // Název složky nebo souboru
        public string FullPath { get; set; } // Celá cesta pro skenování
        public bool IsFile { get; set; } // Je to soubor? (jinak je to složka)

        // Uloženy všechny podsložky a soubory
        public ObservableCollection<FileNode> Children { get; set; } = new ObservableCollection<FileNode>();

        private bool _isChecked = true; // Výchozí stav: vše je zaškrtnuto
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged(nameof(IsChecked));

                    
                    foreach (var child in Children)
                    {
                        child.IsChecked = value;
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
