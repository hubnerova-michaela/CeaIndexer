using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CeaIndexer
{
    public static class CsvAnalyzer
    {
        private static int FindValueColumnIndex(string headerLine)
        {
            var columns = headerLine.Split(';');
            
            for (int i = 0; i < columns.Length; i++)
            {
                var colName = columns[i].Trim().ToLower();
                if (colName == "value" || colName == "hodnota" || colName == "val")
                    return i;
            }

            return 2;
        }

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
            int valueColumnIndex = 2;

            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;

                if (lineNumber <= 5)
                {
                    if (lineNumber == 5)
                        valueColumnIndex = FindValueColumnIndex(line);
                    continue;
                }

                var columns = line.Split(';');

                if (columns.Length <= valueColumnIndex)
                    continue;

                var timeString = columns.Length > 1 ? columns[1] : "";
                var valueString = columns[valueColumnIndex];

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
                int valueColumnIndex = 2;

                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;

                    if (lineNumber <= 5)
                    {
                        if (lineNumber == 5)
                            valueColumnIndex = FindValueColumnIndex(line);
                        continue;
                    }

                    var columns = line.Split(';');

                    if (columns.Length <= valueColumnIndex)
                        continue;

                    var valueString = columns[valueColumnIndex];

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

                if (count == 0)
                {
                    //Logger.Log($"Warning: No valid values found in CSV: {csvPath}");
                    return (0, 0, 0);
                }

                double average = sum / count;
                return (minValue, maxValue, average);
            }
            catch (Exception ex)
            {
                //Logger.LogError($"Error reading CSV {csvPath}", ex);
                return (0, 0, 0);
            }
        }
    }
}
