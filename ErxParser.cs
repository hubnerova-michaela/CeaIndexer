using System;

namespace CeaIndexer
{
    public static class ErxParser
    {
        public static void ParseIdentifyOutput(string erxOutput, FileEntry entry)
        {
            if (string.IsNullOrWhiteSpace(erxOutput))
                return;

            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            var lines = erxOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                int separatorIndex = line.IndexOf(':');
                if (separatorIndex == -1)
                    continue;

                var rawKey = line[..separatorIndex].Trim();
                var key = rawKey.ToLowerInvariant();
                var value = line[(separatorIndex + 1)..].Trim();

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                switch (key)
                {
                    case "identify":
                    case "identified":
                    case "date":
                        if (DateTime.TryParse(value, out DateTime parsedDate))
                            entry.IdentifyDate = parsedDate;
                        break;

                    case "model":
                        entry.Model = value;
                        break;

                    case "record name":
                    case "recordname":
                    case "name":
                        entry.RecordName = value;
                        break;

                    case "record guid":
                    case "recordguid":
                    case "guid":
                        entry.RecordGuid = value;
                        break;

                    case "serial number":
                    case "serialnumber":
                    case "serial":
                        entry.SerialNumber = value;
                        break;

                    case "hardware version":
                    case "hardwareversion":
                    case "hw version":
                    case "hw":
                        entry.HardwareVersion = value;
                        break;

                    case "firmware version":
                    case "firmwareversion":
                    case "fw version":
                    case "fw":
                        entry.FirmwareVersion = value;
                        break;
                }
            }
        }

        public static void ParseQuantitiesOutput(string erxOutput, FileEntry entry)
        {
            if (string.IsNullOrWhiteSpace(erxOutput))
                return;

            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            var lines = erxOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            entry.Quantities.Clear();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (string.IsNullOrEmpty(trimmed) ||
                    trimmed.StartsWith("GROUP:", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("QUANTITIES:", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("[", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith(">") ||
                    trimmed.StartsWith("Exit code") ||
                    trimmed.Contains("erx.exe"))
                {
                    continue;
                }

                var quantityName = trimmed;
                var archive = ExtractArchiveFromQuantityName(quantityName);

                entry.Quantities.Add(new Quantity
                {
                    Name = quantityName,
                    Archive = archive,
                    FileEntry = entry
                });
            }
        }

        private static string ExtractArchiveFromQuantityName(string quantityName)
        {
            if (string.IsNullOrEmpty(quantityName))
                return "main";

            var parts = quantityName.Split('_');
            
            if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0]))
                return parts[0];
            
            return "main";
        }
    }
}
