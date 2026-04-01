using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CeaIndexer.ViewModels
{
    public abstract class CheckableTreeNode<T> : INotifyPropertyChanged where T : CheckableTreeNode<T>
    {
        public string Name { get; set; }

        public ObservableCollection<T> Children { get; set; } = new ObservableCollection<T>();
        private bool _isChecked = false;
        public virtual bool IsChecked
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
