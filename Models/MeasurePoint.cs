using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CeaIndexer.Models
{
    public class MeasurePoint
    {
        [Key]
        public int Id { get; set; }

        // IDENTIFIKACE ZÁZNAMU (z erx.exe -identify)

        [Required]
        public string Guid { get; set; } // Unikátní ID záznamu (Record GUID)

        public string? Name { get; set; } // Uživatelský název

        public string? ObjectPath { get; set; } // Nadřazená složka

        // INFORMACE O HARDWARU (z erx.exe -identify)

        public string? DeviceType { get; set; } // Typ zařízení (Model)

        public string? SerialNumber { get; set; } // Sériové číslo (SN)

        // TECHNICKÁ KONFIGURACE (z erx.exe -list-configs)

        public double? Unom { get; set; } // Jmenovité napětí

        public double? PrimaryCT { get; set; } // Primární proudové trafo
        public double? SecondaryCT { get; set; } // Sekundární proudové trafo

        public double? PrimaryVT { get; set; } // Primární napěťové trafo
        public double? SecondaryVT { get; set; } // Sekundární napěťové trafo

        public string? WiringType { get; set; } // Typ zapojení (Type)

        public string? ConnectionMode { get; set; } // Způsob připojení (cMode)

        public double? Fnom { get; set; } // Jmenovitá frekvence



        public int FileEntryId { get; set; }

        [ForeignKey("FileEntryId")]
        public virtual FileEntry FileEntry { get; set; }

        public virtual ICollection<QuantityItem> Quantities { get; set; } = new List<QuantityItem>();
        public virtual ICollection<TimeInterval> ActivePeriods { get; set; } = new List<TimeInterval>();

    }
}
