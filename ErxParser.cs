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

                var key = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim();

                switch (key)
                {
                    case "Identify":
                        if (DateTime.TryParse(value, out DateTime parsedDate))
                            entry.IdentifyDate = parsedDate;
                        break;
                    case "Model":
                        entry.Model = value;
                        break;
                    case "Record Name":
                        entry.RecordName = value;
                        break;
                    case "Record GUID":
                        entry.RecordGuid = value;
                        break;
                    case "Serial Number":
                        entry.SerialNumber = value;
                        break;
                    case "Hardware Version":
                        entry.HardwareVersion = value;
                        break;
                    case "Firmware Version":
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
                var archive = "main";
                int underscoreIndex = quantityName.IndexOf('_');

                if (underscoreIndex > 0)
                    archive = quantityName[..underscoreIndex];

                entry.Quantities.Add(new Quantity
                {
                    Name = quantityName,
                    Archive = archive,
                    FileEntry = entry
                });
            }
        }
    }
}
