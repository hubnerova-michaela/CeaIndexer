using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CeaIndexer.Models;

public class FileEntry
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string FileName { get; set; }

    [Required]
    public string Path { get; set; }

    public DateTime ScannedAt { get; set; } = DateTime.Now;

    public virtual ICollection<MeasurePoint> MeasurePoints { get; set; } = new List<MeasurePoint>();

}

