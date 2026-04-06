using CeaIndexer.FilterModels;
using CeaIndexer.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CeaIndexer.Helpers
{
    public static class TreeHelper
    {
        public static ObservableCollection<QuantityNode> BuildQuantityTree(IEnumerable<string> rawQuantities)
        {
            var rootNodes = new ObservableCollection<QuantityNode>();

            foreach (var rawName in rawQuantities)
            {
                string[] parts = rawName.Split(new[] { '_', '/' }, StringSplitOptions.RemoveEmptyEntries);

                ObservableCollection<QuantityNode> currentLevel = rootNodes;
                QuantityNode currentNode = null;

                for (int i = 0; i < parts.Length; i++)
                {
                    string partName = parts[i];
                    bool isLastPart = (i == parts.Length - 1);

                    currentNode = currentLevel.FirstOrDefault(n => n.Name == partName);

                    if (currentNode == null)
                    {
                        currentNode = new QuantityNode { Name = partName };
                        currentLevel.Add(currentNode);
                    }

                    if (isLastPart)
                    {
                        currentNode.TechName = rawName;
                    }

                    currentLevel = currentNode.Children;
                }
            }

            return rootNodes;
        }

        // Získání vybraných veličin ze zaškrtnutého stromu
        public static List<QuantityCondition> GetSelectedQuantities(IEnumerable<QuantityNode> nodes, QuantityNode activeFilterNode = null)
        {
            var resultConditions = new List<QuantityCondition>();

            foreach (var node in nodes)
            {
                QuantityNode currentFilter = node.HasActiveFilter ? node : activeFilterNode;


                if (node.IsChecked && node.IsFinalQuantity)
                {
                    var condition = new QuantityCondition
                    {
                        TechName = node.TechName
                    };

                    if (currentFilter != null && currentFilter.HasActiveFilter)
                    {
                        condition.Operator = currentFilter.FilterOperator;
                        condition.Value = currentFilter.FilterValue;

                        CalculateTimeInterval(currentFilter, condition);
                    }

                    resultConditions.Add(condition);
                }

                if (node.Children.Count > 0)
                {
                    resultConditions.AddRange(GetSelectedQuantities(node.Children, currentFilter));
                }
            }

            return resultConditions;
        }

        private static void CalculateTimeInterval(QuantityNode filterNode, QuantityCondition condition)
        {
            DateTime now = DateTime.Now;

            switch (filterNode.TimeFilter)
            {
                case TimeFilterType.Today:
                    condition.IntervalStart = now.Date; // Dnes 00:00:00
                    condition.IntervalEnd = now.Date.AddDays(1).AddTicks(-1); // Dnes 23:59:59
                    break;

                case TimeFilterType.Yesterday:
                    condition.IntervalStart = now.Date.AddDays(-1);
                    condition.IntervalEnd = now.Date.AddTicks(-1);
                    break;

                case TimeFilterType.LastWeek:
                    condition.IntervalStart = now.AddDays(-7);
                    condition.IntervalEnd = now;
                    break;

                case TimeFilterType.LastMonth:
                    condition.IntervalStart = now.AddMonths(-1);
                    condition.IntervalEnd = now;
                    break;

                case TimeFilterType.LastYear:
                    condition.IntervalStart = now.AddYears(-1);
                    condition.IntervalEnd = now;
                    break;

                case TimeFilterType.CustomInterval:
                    condition.IntervalStart = filterNode.CustomStartTime;

                    if (filterNode.CustomEndTime.HasValue)
                    {
                        condition.IntervalEnd = filterNode.CustomEndTime.Value.Date.AddDays(1).AddTicks(-1);
                    }
                    break;

                case TimeFilterType.None:
                default:
                    condition.IntervalStart = null;
                    condition.IntervalEnd = null;
                    break;
            }
        }
    }
}
