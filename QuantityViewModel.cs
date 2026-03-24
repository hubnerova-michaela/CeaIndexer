using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CeaIndexer
{
    public class QuantityViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _name = "";
        private string _archive = "";
        private double? _minValue;
        private double? _maxValue;
        private double? _averageValue;
        private bool _isSelectedAtLoad;
        private bool _isPendingScan;

        public int Id { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Archive
        {
            get => _archive;
            set
            {
                if (_archive != value)
                {
                    _archive = value;
                    OnPropertyChanged();
                }
            }
        }

        public double? MinValue
        {
            get => _minValue;
            set
            {
                if (_minValue != value)
                {
                    _minValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public double? MaxValue
        {
            get => _maxValue;
            set
            {
                if (_maxValue != value)
                {
                    _maxValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public double? AverageValue
        {
            get => _averageValue;
            set
            {
                if (_averageValue != value)
                {
                    _averageValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                    IsPendingScan = _isSelected && !IsSelectedAtLoad;
                }
            }
        }

        public bool IsSelectedAtLoad
        {
            get => _isSelectedAtLoad;
            set
            {
                if (_isSelectedAtLoad != value)
                {
                    _isSelectedAtLoad = value;
                    OnPropertyChanged();
                    IsPendingScan = IsSelected && !_isSelectedAtLoad;
                }
            }
        }

        public bool IsPendingScan
        {
            get => _isPendingScan;
            set
            {
                if (_isPendingScan != value)
                {
                    _isPendingScan = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
