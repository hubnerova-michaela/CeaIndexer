using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CeaIndexer.Helpers
{
    public static class ArchiveMapper
    {
        private static readonly Dictionary<string, string> _mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "main", "Main Archive" },
        { "meter", "Electricity Meter" },
        { "pqmain", "PQ Main" },
        { "log", "Log" },
        { "event", "Voltage Event" },
        { "go", "General Oscillograms" },
        { "hist", "Histogram" },
        { "elog", "Event Log" },
        { "trend", "Trend" }

    };

        public static string GetOfficialName(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return "Other";

            return _mapping.TryGetValue(prefix, out string officialName)
                ? officialName
                : prefix;
        }
    }
}
