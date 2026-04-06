using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CeaIndexer.ViewModels
{
    public enum TimeFilterType
    {
        None,           // Kdykoliv
        Today,          // Dnes
        Yesterday,      // Včera
        LastWeek,       // Poslední týden
        LastMonth,      // Poslední měsíc
        LastYear,       // Poslední rok
        SpecificDate,   // Konkrétní den
        CustomInterval  // Od - Do
    }
}
