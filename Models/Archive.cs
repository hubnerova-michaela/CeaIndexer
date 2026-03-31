using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CeaIndexer.Models
{
    public class Archive
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ArchiveName { get; set; }

        [Required]
        public DateTime StartTime { get; set; } 

        [Required]
        public DateTime EndTime { get; set; }  

        public double? MissingPercentage { get; set; }

        public int MeasurePointId { get; set; }
        [ForeignKey("MeasurePointId")]
        public virtual MeasurePoint MeasurePoint { get; set; }
        public virtual ICollection<QuantityItem> Quantities { get; set; } = new List<QuantityItem>();

    }
}
