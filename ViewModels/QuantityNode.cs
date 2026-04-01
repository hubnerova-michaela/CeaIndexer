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
    }
}
