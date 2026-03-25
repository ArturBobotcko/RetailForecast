using ExcelDataReader;
using Microsoft.VisualBasic.FileIO;
using RetailForecast.DTOs.Dataset;

namespace RetailForecast.Services
{
    public class DatasetPreviewService
    {
        private static readonly string[] CsvDelimiters = [";", ",", "\t", "|"];

        public DatasetPreviewData ParseFilePreview(string filePath, string fileExtension, int rowLimit, int page)
        {
            var columns = new List<string>();
            var rows = new List<Dictionary<string, string>>();
            var totalRows = 0;
            var offset = (page - 1) * rowLimit;

            if (fileExtension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                ParseCsvPreview(filePath, rowLimit, offset, columns, rows, out totalRows);
            }
            else if (fileExtension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                     fileExtension.Equals(".xls", StringComparison.OrdinalIgnoreCase))
            {
                ParseExcelPreview(filePath, rowLimit, offset, columns, rows, out totalRows);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported file format: {fileExtension}");
            }

            return new DatasetPreviewData(columns, rows, totalRows);
        }

        private static void ParseCsvPreview(string filePath, int rowLimit, int offset, List<string> columns,
            List<Dictionary<string, string>> rows, out int totalRows)
        {
            totalRows = 0;
            var headerRead = false;
            var delimiter = DetectCsvDelimiter(filePath);

            using var parser = new TextFieldParser(filePath);
            parser.SetDelimiters(delimiter);
            parser.HasFieldsEnclosedInQuotes = true;
            parser.TrimWhiteSpace = false;

            while (!parser.EndOfData)
            {
                var values = parser.ReadFields();
                if (values == null || values.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                var normalizedValues = values.Select(value => value?.Trim() ?? string.Empty).ToArray();

                if (!headerRead)
                {
                    columns.AddRange(NormalizeHeaders(normalizedValues));
                    headerRead = true;
                    continue;
                }

                totalRows++;
                if (totalRows <= offset || rows.Count >= rowLimit)
                {
                    continue;
                }

                rows.Add(CreateRow(columns, index => index < normalizedValues.Length ? normalizedValues[index] : string.Empty));
            }
        }

        private static void ParseExcelPreview(string filePath, int rowLimit, int offset, List<string> columns,
            List<Dictionary<string, string>> rows, out int totalRows)
        {
            totalRows = 0;
            var headerRead = false;

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            while (reader.Read())
            {
                if (!headerRead)
                {
                    if (IsEmptyRow(reader))
                    {
                        continue;
                    }

                    var headers = Enumerable.Range(0, reader.FieldCount)
                        .Select(index => GetCellValue(reader.GetValue(index)))
                        .ToArray();

                    columns.AddRange(NormalizeHeaders(headers));
                    headerRead = true;
                    continue;
                }

                if (IsEmptyRow(reader))
                {
                    continue;
                }

                totalRows++;
                if (totalRows <= offset || rows.Count >= rowLimit)
                {
                    continue;
                }

                rows.Add(CreateRow(columns, index => index < reader.FieldCount
                    ? GetCellValue(reader.GetValue(index))
                    : string.Empty));
            }
        }

        private static string DetectCsvDelimiter(string filePath)
        {
            using var reader = new StreamReader(filePath);

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                return CsvDelimiters
                    .Select(delimiter => new
                    {
                        Delimiter = delimiter,
                        Score = CountDelimitedFields(line, delimiter)
                    })
                    .OrderByDescending(candidate => candidate.Score)
                    .ThenBy(candidate => Array.IndexOf(CsvDelimiters, candidate.Delimiter))
                    .FirstOrDefault(candidate => candidate.Score > 1)?.Delimiter ?? ",";
            }

            return ",";
        }

        private static int CountDelimitedFields(string line, string delimiter)
        {
            var count = 1;
            var inQuotes = false;

            for (var index = 0; index < line.Length; index++)
            {
                if (line[index] == '"')
                {
                    if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                    {
                        index++;
                        continue;
                    }

                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes && MatchesDelimiter(line, delimiter, index))
                {
                    count++;
                    index += delimiter.Length - 1;
                }
            }

            return count;
        }

        private static bool MatchesDelimiter(string line, string delimiter, int index)
            => index + delimiter.Length <= line.Length &&
               string.Compare(line, index, delimiter, 0, delimiter.Length, StringComparison.Ordinal) == 0;

        private static Dictionary<string, string> CreateRow(IReadOnlyList<string> columns, Func<int, string> valueProvider)
        {
            var row = new Dictionary<string, string>(columns.Count, StringComparer.Ordinal);

            for (var index = 0; index < columns.Count; index++)
            {
                row[columns[index]] = valueProvider(index);
            }

            return row;
        }

        private static List<string> NormalizeHeaders(IEnumerable<string> headers)
        {
            var normalizedHeaders = new List<string>();
            var duplicates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var index = 1;

            foreach (var header in headers)
            {
                var candidate = string.IsNullOrWhiteSpace(header)
                    ? $"Column{index}"
                    : header.Trim();

                if (duplicates.TryGetValue(candidate, out var count))
                {
                    count++;
                    duplicates[candidate] = count;
                    candidate = $"{candidate}_{count}";
                }
                else
                {
                    duplicates[candidate] = 1;
                }

                normalizedHeaders.Add(candidate);
                index++;
            }

            return normalizedHeaders;
        }

        private static bool IsEmptyRow(IExcelDataReader reader)
            => Enumerable.Range(0, reader.FieldCount)
                .All(index => string.IsNullOrWhiteSpace(GetCellValue(reader.GetValue(index))));

        private static string GetCellValue(object? value)
            => value?.ToString()?.Trim() ?? string.Empty;
    }
}
