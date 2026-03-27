using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CeaIndexer.Models
{
    public class TimeInterval
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ArchiveName { get; set; } // "Main Archive", "Electricity Meter"

        [Required]
        public DateTime StartTime { get; set; } // Začátek bloku, kdy přístroj měřil

        [Required]
        public DateTime EndTime { get; set; }   // Konec bloku, kdy se přístroj vypnul

        public double MissingPercentage { get; set; } 



        public int MeasurePointId { get; set; }

        [ForeignKey("MeasurePointId")]
        public virtual MeasurePoint MeasurePoint { get; set; }
    }
}
