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
        // vytvoření stromu z názvů veličin
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

        // získání vybraných veličin ze zaškrtnutého stromu
        public static List<string> GetSelectedQuantities(ObservableCollection<QuantityNode> nodes)
        {
            List<string> selectedTechNames = new List<string>();

            foreach (var node in nodes)
            {
                if (node.IsChecked && node.IsFinalQuantity)
                {
                    selectedTechNames.Add(node.TechName);
                }

                if (node.Children.Count > 0)
                {
                    selectedTechNames.AddRange(GetSelectedQuantities(node.Children));
                }
            }

            return selectedTechNames;
        }
    }
}
