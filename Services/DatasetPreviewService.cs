using ExcelDataReader;
using Microsoft.VisualBasic.FileIO;
using RetailForecast.DTOs.Dataset;
using System.Globalization;

namespace RetailForecast.Services
{
    public class DatasetPreviewService
    {
        private static readonly string[] CsvDelimiters = [";", ",", "\t", "|"];
        private const double StrongCorrelationThreshold = 0.85;
        private static readonly string[] TimeColumnKeywords = ["год", "year", "date", "time", "timestamp", "period", "квартал", "quarter", "месяц", "month"];

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

        public DatasetFeatureAnalysisResponse ParseFeatureAnalysis(
            string filePath,
            string fileExtension,
            string fileName)
        {
            var (columns, rows) = ParseFileRows(filePath, fileExtension);
            var numericColumns = GetNumericColumns(columns, rows);
            var correlationMatrix = BuildCorrelationMatrix(numericColumns, rows, columns);
            var strongCorrelations = BuildStrongCorrelations(correlationMatrix);

            return new DatasetFeatureAnalysisResponse(
                fileName,
                columns,
                numericColumns,
                correlationMatrix,
                strongCorrelations);
        }

        private static (List<string> Columns, List<string[]> Rows) ParseFileRows(string filePath, string fileExtension)
        {
            var columns = new List<string>();
            var rows = new List<string[]>();

            if (fileExtension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                ParseCsvRows(filePath, columns, rows);
            }
            else if (fileExtension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                     fileExtension.Equals(".xls", StringComparison.OrdinalIgnoreCase))
            {
                ParseExcelRows(filePath, columns, rows);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported file format: {fileExtension}");
            }

            return (columns, rows);
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

        private static void ParseCsvRows(string filePath, List<string> columns, List<string[]> rows)
        {
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

                rows.Add(normalizedValues);
            }
        }

        private static void ParseExcelRows(string filePath, List<string> columns, List<string[]> rows)
        {
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

                rows.Add(Enumerable.Range(0, columns.Count)
                    .Select(index => index < reader.FieldCount ? GetCellValue(reader.GetValue(index)) : string.Empty)
                    .ToArray());
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

        private static List<string> GetNumericColumns(IReadOnlyList<string> columns, IReadOnlyList<string[]> rows)
        {
            var numericColumns = new List<string>();

            for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                if (IsTimeLikeColumn(columns[columnIndex]))
                {
                    continue;
                }

                var nonEmptyCount = 0;
                var numericCount = 0;

                foreach (var row in rows)
                {
                    if (columnIndex >= row.Length)
                    {
                        continue;
                    }

                    var value = row[columnIndex];
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    nonEmptyCount++;
                    if (TryParseNumeric(value, out _))
                    {
                        numericCount++;
                    }
                }

                if (nonEmptyCount > 0 && nonEmptyCount == numericCount)
                {
                    numericColumns.Add(columns[columnIndex]);
                }
            }

            return numericColumns;
        }

        private static bool IsTimeLikeColumn(string columnName)
        {
            var normalizedColumnName = columnName.Trim().ToLowerInvariant();
            return TimeColumnKeywords.Any(keyword => normalizedColumnName.Contains(keyword, StringComparison.Ordinal));
        }

        private static Dictionary<string, Dictionary<string, double?>> BuildCorrelationMatrix(
            IReadOnlyList<string> numericColumns,
            IReadOnlyList<string[]> rows,
            IReadOnlyList<string> allColumns)
        {
            var matrix = new Dictionary<string, Dictionary<string, double?>>(StringComparer.Ordinal);
            var columnIndexMap = allColumns
                .Select((column, index) => new { column, index })
                .ToDictionary(item => item.column, item => item.index, StringComparer.Ordinal);

            foreach (var leftColumn in numericColumns)
            {
                var rowValues = new Dictionary<string, double?>(StringComparer.Ordinal);

                foreach (var rightColumn in numericColumns)
                {
                    if (string.Equals(leftColumn, rightColumn, StringComparison.Ordinal))
                    {
                        rowValues[rightColumn] = 1.0;
                        continue;
                    }

                    var leftValues = new List<double>();
                    var rightValues = new List<double>();
                    var leftIndex = columnIndexMap[leftColumn];
                    var rightIndex = columnIndexMap[rightColumn];

                    foreach (var row in rows)
                    {
                        if (leftIndex >= row.Length || rightIndex >= row.Length)
                        {
                            continue;
                        }

                        if (!TryParseNumeric(row[leftIndex], out var leftValue) ||
                            !TryParseNumeric(row[rightIndex], out var rightValue))
                        {
                            continue;
                        }

                        leftValues.Add(leftValue);
                        rightValues.Add(rightValue);
                    }

                    rowValues[rightColumn] = CalculatePearsonCorrelation(leftValues, rightValues);
                }

                matrix[leftColumn] = rowValues;
            }

            return matrix;
        }

        private static List<DatasetFeatureCorrelationResponse> BuildStrongCorrelations(
            IReadOnlyDictionary<string, Dictionary<string, double?>> correlationMatrix)
        {
            var strongCorrelations = new List<DatasetFeatureCorrelationResponse>();
            var numericColumns = correlationMatrix.Keys.OrderBy(column => column, StringComparer.Ordinal).ToList();

            for (var leftIndex = 0; leftIndex < numericColumns.Count; leftIndex++)
            {
                for (var rightIndex = leftIndex + 1; rightIndex < numericColumns.Count; rightIndex++)
                {
                    var leftColumn = numericColumns[leftIndex];
                    var rightColumn = numericColumns[rightIndex];
                    var correlation = correlationMatrix[leftColumn][rightColumn];

                    if (!correlation.HasValue || Math.Abs(correlation.Value) < StrongCorrelationThreshold)
                    {
                        continue;
                    }

                    var absoluteCorrelation = Math.Abs(correlation.Value);
                    strongCorrelations.Add(new DatasetFeatureCorrelationResponse(
                        leftColumn,
                        rightColumn,
                        Math.Round(correlation.Value, 4),
                        Math.Round(absoluteCorrelation, 4),
                        absoluteCorrelation >= 0.95 ? "high" : "medium"));
                }
            }

            return strongCorrelations
                .OrderByDescending(item => item.AbsoluteCorrelation)
                .ThenBy(item => item.LeftColumn, StringComparer.Ordinal)
                .ThenBy(item => item.RightColumn, StringComparer.Ordinal)
                .ToList();
        }

        private static double? CalculatePearsonCorrelation(IReadOnlyList<double> leftValues, IReadOnlyList<double> rightValues)
        {
            if (leftValues.Count != rightValues.Count || leftValues.Count < 2)
            {
                return null;
            }

            var leftMean = leftValues.Average();
            var rightMean = rightValues.Average();
            double numerator = 0;
            double leftDenominator = 0;
            double rightDenominator = 0;

            for (var index = 0; index < leftValues.Count; index++)
            {
                var leftDelta = leftValues[index] - leftMean;
                var rightDelta = rightValues[index] - rightMean;

                numerator += leftDelta * rightDelta;
                leftDenominator += leftDelta * leftDelta;
                rightDenominator += rightDelta * rightDelta;
            }

            if (leftDenominator <= 0 || rightDenominator <= 0)
            {
                return null;
            }

            return numerator / Math.Sqrt(leftDenominator * rightDenominator);
        }

        private static bool TryParseNumeric(string value, out double result)
        {
            var normalized = value.Trim()
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("%", string.Empty, StringComparison.Ordinal);

            return double.TryParse(normalized, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result) ||
                   double.TryParse(normalized.Replace(',', '.'), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result) ||
                   double.TryParse(normalized, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.GetCultureInfo("ru-RU"), out result);
        }
    }
}
