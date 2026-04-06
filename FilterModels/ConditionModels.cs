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
        LessThan,
        Exists,
        NotExists
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

    // SKUPINA
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

    // PRAVIDLO (ŘÁDEK)
    public class ConditionRule : IConditionNode, INotifyPropertyChanged
    {

        public static Dictionary<string, List<string>> GlobalAutocompleteCache { get; set; } = new Dictionary<string, List<string>>();


        public ObservableCollection<PropertyDisplayItem> AvailableProperties { get; } = new ObservableCollection<PropertyDisplayItem>();

        private ObservableCollection<EnumDisplayItem> _availableOperators = new ObservableCollection<EnumDisplayItem>();
        public ObservableCollection<EnumDisplayItem> AvailableOperators
        {
            get => _availableOperators;
            set { _availableOperators = value; OnPropertyChanged(); }
        }

        private ObservableCollection<string> _availableValues = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableValues
        {
            get => _availableValues;
            set { _availableValues = value; OnPropertyChanged(); }
        }


        private bool _useDropdownForValue;
        public bool UseDropdownForValue
        {
            get => _useDropdownForValue;
            set { _useDropdownForValue = value; OnPropertyChanged(); }
        }

        private bool _useDatePickerForValue;
        public bool UseDatePickerForValue
        {
            get => _useDatePickerForValue;
            set
            {
                if (_useDatePickerForValue != value)
                {
                    _useDatePickerForValue = value;
                    OnPropertyChanged(nameof(UseDatePickerForValue));
                }
            }
        }

        private RuleCategory _category;
        public RuleCategory Category
        {
            get => _category;
            set
            {
                if (_category != value)
                {
                    _category = value;
                    OnPropertyChanged();
                    UpdateAvailableProperties(); // Vygeneruje nové vlastnosti
                }
            }
        }

        private string _targetProperty;
        public string TargetProperty
        {
            get => _targetProperty;
            set
            {
                if (_targetProperty != value)
                {
                    _targetProperty = value;
                    OnPropertyChanged();
                    UpdateOperatorsAndInputMode();
                }
            }
        }

        private RelationalOperator _operator;
        public RelationalOperator Operator
        {
            get => _operator;
            set
            {
                if (_operator != value)
                {
                    _operator = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedOperator));
                    UpdateInputMode();
                }
            }
        }

        public object SelectedOperator
        {
            get => _operator;
            set
            {

                if (value is RelationalOperator op)
                {
                    Operator = op;
                }
            }
        }

        private string _value;
        public string Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

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
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "Guid", DisplayName = "GUID záznamu" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "ObjectPath", DisplayName = "Nadřazená složka" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "Name", DisplayName = "Uživatelský název" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "DeviceType", DisplayName = "Typ zařízení (Model)" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "SerialNumber", DisplayName = "Sériové číslo" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "WiringType", DisplayName = "Typ zapojení" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "ConnectionMode", DisplayName = "Způsob připojení" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "Unom", DisplayName = "Jmenovité napětí (Unom)" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "Fnom", DisplayName = "Frekvence (Fnom)" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "PrimaryCT", DisplayName = "Primární trafo proudu (CT)" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "SecondaryCT", DisplayName = "Sekundární trafo proudu (CT)" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "PrimaryVT", DisplayName = "Primární trafo napětí (VT)" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "SecondaryVT", DisplayName = "Sekundární trafo napětí (VT)" });
                    break;

                case RuleCategory.Quantity:
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "main_U_avg_U1", DisplayName = "main_U_avg_U1" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "main_U_avg_U2", DisplayName = "main_U_avg_U2" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "main_I_avg_I1", DisplayName = "main_I_avg_I1" });
                    break;

                case RuleCategory.Archive:
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "StartTime", DisplayName = "Čas začátku (Od)" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "EndTime", DisplayName = "Čas konce (Do)" });
                    AvailableProperties.Add(new PropertyDisplayItem { Key = "ArchiveName", DisplayName = "Název archivu (např. PQ)" });
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


        private void UpdateOperatorsAndInputMode()
        {
            if (string.IsNullOrEmpty(TargetProperty)) return;

            var allowedOperators = new List<RelationalOperator>();
            var textProperties = new[] { "FileName", "Path", "Name", "SerialNumber", "WiringType", "ConnectionMode", "ObjectPath", "Guid" };
            var exactMatchProperties = new[] { "DeviceType", "ArchiveName" };

            if (Category == RuleCategory.Quantity)
                allowedOperators.AddRange(new[] { RelationalOperator.GreaterThan, RelationalOperator.LessThan, RelationalOperator.Equals, RelationalOperator.Exists, RelationalOperator.NotExists });
            else if (textProperties.Contains(TargetProperty))
                allowedOperators.AddRange(new[] { RelationalOperator.Equals, RelationalOperator.Contains });
            else if (exactMatchProperties.Contains(TargetProperty))
                allowedOperators.Add(RelationalOperator.Equals);
            else
                allowedOperators.AddRange(new[] { RelationalOperator.Equals, RelationalOperator.GreaterThan, RelationalOperator.LessThan });


            var newOperators = new ObservableCollection<EnumDisplayItem>();
            foreach (var op in allowedOperators)
            {
                string opName = op switch
                {
                    RelationalOperator.Equals => "Rovná se",
                    RelationalOperator.Contains => "Obsahuje text",
                    RelationalOperator.GreaterThan => "Větší než",
                    RelationalOperator.LessThan => "Menší než",
                    RelationalOperator.Exists => "Existuje",
                    RelationalOperator.NotExists => "Neexistuje",
                    _ => op.ToString()
                };
                newOperators.Add(new EnumDisplayItem { Value = op, DisplayName = opName });
            }


            AvailableOperators = newOperators;


            if (!allowedOperators.Contains(Operator) && allowedOperators.Count > 0)
            {
                Operator = allowedOperators[0];
            }
            else
            {

                OnPropertyChanged(nameof(SelectedOperator));
                OnPropertyChanged(nameof(Operator));
            }

            UpdateInputMode();
        }


        private void UpdateInputMode()
        {
            var dropDownProperties = new[] { "FileName", "Path", "DeviceType", "ArchiveName", "SerialNumber", "WiringType", "Name", "ConnectionMode", "ObjectPath" };
            var dateProperties = new[] { "ScannedAt", "StartTime", "EndTime" };

            bool isDate = dateProperties.Contains(TargetProperty);
            bool isDropdown = Operator == RelationalOperator.Equals && dropDownProperties.Contains(TargetProperty);


            UseDatePickerForValue = isDate;
            UseDropdownForValue = isDropdown;

            if (isDropdown)
            {
                var newValues = new ObservableCollection<string>();
                if (TargetProperty != null && GlobalAutocompleteCache != null && GlobalAutocompleteCache.TryGetValue(TargetProperty, out var cachedList))
                {
                    foreach (var item in cachedList)
                    {
                        if (!string.IsNullOrWhiteSpace(item)) newValues.Add(item);
                    }
                }
                AvailableValues = newValues;
            }

            OnPropertyChanged(nameof(UseDatePickerForValue));
            OnPropertyChanged(nameof(UseDropdownForValue));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}