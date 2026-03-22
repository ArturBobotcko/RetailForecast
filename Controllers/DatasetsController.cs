using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailForecast.DTOs.Dataset;
using RetailForecast.Services;

namespace RetailForecast.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatasetsController : ControllerBase
    {
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
                var userIdString = Request.Form["userId"].FirstOrDefault();

                if (!int.TryParse(userIdString, out var userId))
                    return BadRequest(new { message = "Valid UserId is required" });

                var request = new CreateDatasetRequest(
                    File: file,
                    OriginalFileName: file?.FileName,
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
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "No file provided" });

                var dataset = await _service.GetByIdAsync(id, ct);
                if (dataset == null)
                    return NotFound(new { message = "Dataset not found" });

                var storageFileName = await _fileStorageService.SaveFileAsync(file, dataset.UserId);
                var storageFilePath = _fileStorageService.GetStorageFilePath(dataset.UserId, storageFileName);
                var fileSizeBytes = _fileStorageService.GetFileSizeBytes(storageFilePath);

                return Ok(new
                {
                    id = dataset.Id,
                    storageFileName,
                    fileSizeBytes,
                    fileExtension = Path.GetExtension(storageFileName)
                });
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

                return File(fileStream, "application/octet-stream", dataset.OriginalFileName);
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
        public async Task<IActionResult> GetPreview(int id, [FromQuery] int rows = 100, CancellationToken ct = default)
        {
            try
            {
                var dataset = await _service.GetByIdAsync(id, ct);
                if (dataset == null)
                    return NotFound(new { message = "Dataset not found" });

                if (string.IsNullOrEmpty(dataset.StorageFileName))
                    return BadRequest(new { message = "No file associated with this dataset" });

                var filePath = _fileStorageService.GetStorageFilePath(dataset.UserId, dataset.StorageFileName);

                // Parse CSV or Excel file
                var previewData = ParseFilePreview(filePath, dataset.FileExtension, rows);
                
                return Ok(new
                {
                    dataset.OriginalFileName,
                    columns = previewData.Columns,
                    rows = previewData.Rows,
                    totalRows = previewData.TotalRows,
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
            string filePath, string fileExtension, int rowLimit)
        {
            var columns = new List<string>();
            var rows = new List<Dictionary<string, string>>();
            int totalRows = 0;

            if (fileExtension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                ParseCsvPreview(filePath, rowLimit, columns, rows, out totalRows);
            }
            else if (fileExtension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                     fileExtension.Equals(".xls", StringComparison.OrdinalIgnoreCase))
            {
                ParseExcelPreview(filePath, rowLimit, columns, rows, out totalRows);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported file format: {fileExtension}");
            }

            return (columns, rows, totalRows);
        }

        private void ParseCsvPreview(string filePath, int rowLimit, List<string> columns,
            List<Dictionary<string, string>> rows, out int totalRows)
        {
            totalRows = 0;
            bool headerRead = false;

            using (var reader = new StreamReader(filePath))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var values = line.Split(',');

                    if (!headerRead)
                    {
                        columns.AddRange(values.Select(v => v.Trim('"').Trim()));
                        headerRead = true;
                    }
                    else
                    {
                        if (rows.Count >= rowLimit)
                            break;

                        var row = new Dictionary<string, string>();
                        for (int i = 0; i < columns.Count && i < values.Length; i++)
                        {
                            row[columns[i]] = values[i].Trim('"').Trim();
                        }
                        rows.Add(row);
                    }

                    totalRows++;
                }
            }
        }

        private void ParseExcelPreview(string filePath, int rowLimit, List<string> columns,
            List<Dictionary<string, string>> rows, out int totalRows)
        {
            totalRows = 0;
            
            // For now, return empty preview for Excel - requires additional NuGet package
            throw new NotImplementedException("Excel preview is not yet implemented. Please use CSV files for now.");
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