using System;
using System.Globalization;
using System.IO;

namespace CeaIndexer
{
    public static class CsvAnalyzer
    {
        public static (double Min, double Max, DateTime MinTime, DateTime MaxTime) AnalyzeValues(string csvPath)
        {
            if (string.IsNullOrWhiteSpace(csvPath))
                throw new ArgumentException("CSV path cannot be null or empty.", nameof(csvPath));

            if (!File.Exists(csvPath))
                throw new FileNotFoundException($"CSV file not found: {csvPath}");

            double minValue = double.MaxValue;
            double maxValue = double.MinValue;
            DateTime minTime = DateTime.MinValue;
            DateTime maxTime = DateTime.MinValue;

            using var reader = new StreamReader(csvPath);
            int lineNumber = 0;
            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;

                if (lineNumber <= 5)
                    continue;

                var columns = line.Split(';');

                if (columns.Length < 3)
                    continue;

                var timeString = columns[1];
                var valueString = columns[2];

                if (double.TryParse(valueString, NumberStyles.Any, CultureInfo.InvariantCulture, out double value) &&
                    DateTime.TryParse(timeString, out DateTime time))
                {
                    if (value < minValue)
                    {
                        minValue = value;
                        minTime = time;
                    }

                    if (value > maxValue)
                    {
                        maxValue = value;
                        maxTime = time;
                    }
                }
            }

            return (minValue, maxValue, minTime, maxTime);
        }

        public static (double MinValue, double MaxValue, double AverageValue) ExtractStatisticsFromCsv(string csvPath)
        {
            if (!File.Exists(csvPath))
                return (0, 0, 0);

            double minValue = double.MaxValue;
            double maxValue = double.MinValue;
            double sum = 0;
            int count = 0;

            try
            {
                using var reader = new StreamReader(csvPath);
                int lineNumber = 0;
                string? line;

                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;

                    if (lineNumber <= 5)
                        continue;

                    var columns = line.Split(';');

                    if (columns.Length < 3)
                        continue;

                    var valueString = columns[2];

                    if (double.TryParse(valueString, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
                    {
                        if (value < minValue)
                            minValue = value;

                        if (value > maxValue)
                            maxValue = value;

                        sum += value;
                        count++;
                    }
                }

                double average = count > 0 ? sum / count : 0;
                return (minValue == double.MaxValue ? 0 : minValue, maxValue == double.MinValue ? 0 : maxValue, average);
            }
            catch
            {
                return (0, 0, 0);
            }
        }
    }
}
