using CeaIndexer.FilterModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CeaIndexer.ViewModels
{
    public class QuantityNode : CheckableTreeNode<QuantityNode>
    {
        public string TechName { get; set; }

        public bool IsFinalQuantity => !string.IsNullOrEmpty(TechName);

        private RelationalOperator? _filterOperator;
        public RelationalOperator? FilterOperator
        {
            get => _filterOperator;
            set { _filterOperator = value; OnPropertyChanged(nameof(FilterOperator)); OnPropertyChanged(nameof(HasActiveFilter)); }
        }

        private string _filterValue;
        public string FilterValue
        {
            get => _filterValue;
            set { _filterValue = value; OnPropertyChanged(nameof(FilterValue)); OnPropertyChanged(nameof(HasActiveFilter)); }
        }

        private TimeFilterType _timeFilter = TimeFilterType.None;
        public TimeFilterType TimeFilter
        {
            get => _timeFilter;
            set { _timeFilter = value; OnPropertyChanged(nameof(TimeFilter)); OnPropertyChanged(nameof(IsCustomTimeSelected)); }
        }

        private DateTime? _customStartTime;
        public DateTime? CustomStartTime
        {
            get => _customStartTime;
            set { _customStartTime = value; OnPropertyChanged(nameof(CustomStartTime)); }
        }

        private DateTime? _customEndTime;
        public DateTime? CustomEndTime
        {
            get => _customEndTime;
            set { _customEndTime = value; OnPropertyChanged(nameof(CustomEndTime)); }
        }

        public bool HasActiveFilter => FilterOperator != null && !string.IsNullOrWhiteSpace(FilterValue);

        public bool IsCustomTimeSelected => TimeFilter == TimeFilterType.CustomInterval;
    }
}
