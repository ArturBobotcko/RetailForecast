using ExcelDataReader;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic.FileIO;
using RetailForecast.DTOs.Dataset;
using RetailForecast.Services;

namespace RetailForecast.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatasetsController : ControllerBase
    {
        private const int DefaultPreviewRows = 10;
        private static readonly string[] CsvDelimiters = [";", ",", "\t", "|"];

        private readonly DatasetService _service;
        private readonly FileStorageService _fileStorageService;
        private readonly ILogger<DatasetsController> _logger;

        public DatasetsController(
            DatasetService service,
            FileStorageService fileStorageService,
            ILogger<DatasetsController> logger)
        {
            _service = service;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll(CancellationToken ct)
            => Ok(await _service.GetAllAsync(ct));

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _service.GetByIdAsync(id, ct);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            try
            {
                var file = Request.Form.Files.FirstOrDefault();
                var description = Request.Form["description"].FirstOrDefault();
                var originalFileName = Request.Form["originalFileName"].FirstOrDefault();
                var userIdString = Request.Form["userId"].FirstOrDefault();

                if (!int.TryParse(userIdString, out var userId))
                    return BadRequest(new { message = "Valid UserId is required" });

                var request = new CreateDatasetRequest(
                    File: file,
                    OriginalFileName: originalFileName,
                    Description: description,
                    UserId: userId);

                var result = await _service.CreateAsync(request, ct);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating dataset: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while uploading the file" });
            }
        }

        [HttpPost("{id}/upload")]
        [Authorize]
        public async Task<IActionResult> Upload(int id, CancellationToken ct)
        {
            try
            {
                var file = Request.Form.Files.FirstOrDefault();
                var description = Request.Form["description"].FirstOrDefault();
                var originalFileName = Request.Form["originalFileName"].FirstOrDefault();

                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "No file provided" });

                var result = await _service.ReplaceFileAsync(id, file, originalFileName, description, ct);
                if (result == null)
                    return NotFound(new { message = "Dataset not found" });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading file: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while uploading the file" });
            }
        }

        [HttpGet("{id}/download")]
        [Authorize]
        public async Task<IActionResult> Download(int id, CancellationToken ct)
        {
            try
            {
                var dataset = await _service.GetByIdAsync(id, ct);
                if (dataset == null)
                    return NotFound(new { message = "Dataset not found" });

                if (string.IsNullOrEmpty(dataset.StorageFileName))
                    return BadRequest(new { message = "No file associated with this dataset" });

                var filePath = _fileStorageService.GetStorageFilePath(dataset.UserId, dataset.StorageFileName);
                var fileStream = _fileStorageService.GetFileStream(filePath);
                var downloadFileName = BuildDownloadFileName(dataset.OriginalFileName, dataset.FileExtension);

                return File(fileStream, "application/octet-stream", downloadFileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { message = "File not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading file: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while downloading the file" });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(
            int id, [FromBody] UpdateDatasetRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _service.UpdateAsync(id, request, ct);
                if (result is null)
                    return NotFound(new { message = "Dataset not found or no fields to update" });
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}/preview")]
        [Authorize]
        public async Task<IActionResult> GetPreview(
            int id,
            [FromQuery] int rows = DefaultPreviewRows,
            [FromQuery] int page = 1,
            CancellationToken ct = default)
        {
            try
            {
                var dataset = await _service.GetByIdAsync(id, ct);
                if (dataset == null)
                    return NotFound(new { message = "Dataset not found" });

                if (string.IsNullOrEmpty(dataset.StorageFileName))
                    return BadRequest(new { message = "No file associated with this dataset" });

                var filePath = _fileStorageService.GetStorageFilePath(dataset.UserId, dataset.StorageFileName);
                var previewRows = rows > 0 ? rows : DefaultPreviewRows;
                var previewPage = page > 0 ? page : 1;
                var previewData = ParseFilePreview(filePath, dataset.FileExtension, previewRows, previewPage);

                return Ok(new
                {
                    dataset.OriginalFileName,
                    columns = previewData.Columns,
                    rows = previewData.Rows,
                    totalRows = previewData.TotalRows,
                    currentPage = previewPage,
                    pageSize = previewRows,
                    preview = true
                });
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { message = "File not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting preview: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        private (List<string> Columns, List<Dictionary<string, string>> Rows, int TotalRows) ParseFilePreview(
            string filePath, string fileExtension, int rowLimit, int page)
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

            return (columns, rows, totalRows);
        }

        private void ParseCsvPreview(string filePath, int rowLimit, int offset, List<string> columns,
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

        private void ParseExcelPreview(string filePath, int rowLimit, int offset, List<string> columns,
            List<Dictionary<string, string>> rows, out int totalRows)
        {
            totalRows = 0;
            var headerRead = false;

            using var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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

        private static string BuildDownloadFileName(string datasetName, string fileExtension)
        {
            if (!string.IsNullOrWhiteSpace(fileExtension) &&
                !datasetName.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase))
            {
                return $"{datasetName}{fileExtension}";
            }

            return datasetName;
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var deleted = await _service.DeleteAsync(id, ct);
            return deleted ? NoContent() : NotFound();
        }
    }
}

