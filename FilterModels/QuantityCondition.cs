using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CeaIndexer.FilterModels
{
    public class QuantityCondition
    {
        public string TechName { get; set; } // např. "main_U1"

        // Volitelné filtry
        public RelationalOperator? Operator { get; set; }
        public string Value { get; set; }

        // Přesně spočítané časy (už převedené např. z "Dnes" na konkrétní datumy)
        public DateTime? IntervalStart { get; set; }
        public DateTime? IntervalEnd { get; set; }
    }
}
