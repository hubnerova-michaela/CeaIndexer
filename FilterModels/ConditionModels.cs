using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CeaIndexer.FilterModels
{

    // POMOCNÉ TŘÍDY PRO PŘEKLADY (Tohle ti tam chybělo)
    public class EnumDisplayItem
    {
        public object Value { get; set; }
        public string DisplayName { get; set; }
    }

    public class PropertyDisplayItem
    {
        public string Key { get; set; }
        public string DisplayName { get; set; }
    }


    // Výčtové typy (Enums) - Obsah roletek
    public enum RuleCategory
    {
        File,     
        Device,    
        Archive,    
        Quantity    
    }

    public enum RelationalOperator
    {
        Equals,        
        Contains,      
        GreaterThan,  
        LessThan     
    }

    public enum LogicalOperator
    {
        And, 
        Or  
    }

  
    // společný předek pro Skupinu i samotný Řádek
    public interface IConditionNode
    {
    }


    // SKUPINA (např. "Splnit VŠECHNY z následujících:")
    public class ConditionGroup : IConditionNode, INotifyPropertyChanged
    {
        private LogicalOperator _logicOperator;
        public LogicalOperator LogicOperator
        {
            get => _logicOperator;
            set { _logicOperator = value; OnPropertyChanged(); }
        }

        public ObservableCollection<IConditionNode> Children { get; set; } = new ObservableCollection<IConditionNode>();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }


    // 4. SAMOTNÉ PRAVIDLO (řádek s roletkami)
    public class ConditionRule : IConditionNode, INotifyPropertyChanged
    {
        private RuleCategory _category;
        private string _targetProperty;
        private RelationalOperator _operator;
        private string _value;

        public RuleCategory Category
        {
            get => _category;
            set
            {
                if (_category != value)
                {
                    _category = value;
                    OnPropertyChanged();
                    UpdateAvailableProperties();
                }
            }
        }

        public string TargetProperty
        {
            get => _targetProperty;
            set { _targetProperty = value; OnPropertyChanged(); }
        }

        public RelationalOperator Operator
        {
            get => _operator;
            set { _operator = value; OnPropertyChanged(); }
        }

        public string Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

        public ObservableCollection<PropertyDisplayItem> AvailableProperties { get; } = new ObservableCollection<PropertyDisplayItem>();

        public ConditionRule()
        {
            _category = RuleCategory.File;
            UpdateAvailableProperties();
        }

        private void UpdateAvailableProperties()
        {
            AvailableProperties.Clear();

            switch (Category)
            {
                case RuleCategory.File:
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "FileName", DisplayName = "Název souboru" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "Path", DisplayName = "Cesta k souboru" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "ScannedAt", DisplayName = "Datum načtení (Skriptu)" });
                    break;

                case RuleCategory.Device:
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "Name", DisplayName = "Uživatelský název" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "DeviceType", DisplayName = "Typ zařízení (Model)" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "SerialNumber", DisplayName = "Sériové číslo" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "Unom", DisplayName = "Jmenovité napětí (Unom)" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "Fnom", DisplayName = "Frekvence (Fnom)" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "WiringType", DisplayName = "Typ zapojení" });
                    break;

                case RuleCategory.Quantity:
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "main_U_avg_U1", DisplayName = "main_U_avg_U1" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "main_U_avg_U2", DisplayName = "main_U_avg_U2" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "main_I_avg_I1", DisplayName = "main_I_avg_I1" });
                    break;
            }

            if (AvailableProperties.Count > 0)
            {
                TargetProperty = AvailableProperties[0].Key;
            }
            else
            {
                TargetProperty = string.Empty;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}