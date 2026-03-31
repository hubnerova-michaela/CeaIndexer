using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CeaIndexer.Models
{
    public class QuantityItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int ArchiveId { get; set; }

        [ForeignKey("ArchiveId")]
        public virtual Archive Archive { get; set; }
        public double? GlobalMinValue { get; set; }
        public double? GlobalMaxValue { get; set; }
        public double? GlobalAvg { get; set; }

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}
