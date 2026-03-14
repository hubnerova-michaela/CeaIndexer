using System;
using System.Collections.Generic;

namespace CeaIndexer;

public class FileEntry
{
    public int Id { get; set; }
    public string Path { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime IndexedAt { get; set; }

    public DateTime? IdentifyDate { get; set; }
    public string? Model { get; set; }
    public string? RecordName { get; set; }
    public string? RecordGuid { get; set; }
    public string? SerialNumber { get; set; }
    public string? HardwareVersion { get; set; }
    public string? FirmwareVersion { get; set; }
    public List<Quantity> Quantities { get; set; } = new();
}

