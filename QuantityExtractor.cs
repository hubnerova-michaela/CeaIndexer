using System;
using System.Collections.Generic;
using System.Linq;

namespace CeaIndexer
{
    public static class QuantityExtractor
    {
        public static string BuildQuantitiesParameter(List<Quantity> fileQuantities, string[] watchlist)
        {
            if (watchlist == null || watchlist.Length == 0)
                return string.Empty;

            var existingWatchlistItems = fileQuantities
                .Where(q => watchlist.Contains(q.Name))
                .Select(q => q.Name)
                .ToList();

            if (existingWatchlistItems.Count == 0)
                return string.Empty;

            return string.Join(";", existingWatchlistItems);
        }

        public static void AssignStatisticsToQuantities(
            List<Quantity> quantities,
            double minValue,
            double maxValue,
            double averageValue)
        {
            foreach (var q in quantities)
            {
                if (q.MinValue == null)
                    q.MinValue = minValue;

                if (q.MaxValue == null)
                    q.MaxValue = maxValue;

                if (q.AverageValue == null)
                    q.AverageValue = averageValue;
            }
        }
    }
}
