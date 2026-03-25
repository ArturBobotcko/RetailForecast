using System.Security.Claims;
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
        private const int DefaultPreviewRows = 10;

        private readonly DatasetService _service;
        private readonly DatasetPreviewService _datasetPreviewService;
        private readonly FileStorageService _fileStorageService;
        private readonly ILogger<DatasetsController> _logger;

        public DatasetsController(
            DatasetService service,
            DatasetPreviewService datasetPreviewService,
            FileStorageService fileStorageService,
            ILogger<DatasetsController> logger)
        {
            _service = service;
            _datasetPreviewService = datasetPreviewService;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            if (!TryGetCurrentUserId(out var userId))
                return Unauthorized(new { message = "Invalid user token" });

            return Ok(await _service.GetAllAsync(userId, ct));
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            if (!TryGetCurrentUserId(out var userId))
                return Unauthorized(new { message = "Invalid user token" });

            var result = await _service.GetByIdAsync(id, userId, ct);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            try
            {
                if (!TryGetCurrentUserId(out var userId))
                    return Unauthorized(new { message = "Invalid user token" });

                var request = new CreateDatasetRequest(
                    File: Request.Form.Files.FirstOrDefault(),
                    OriginalFileName: Request.Form["originalFileName"].FirstOrDefault(),
                    Description: Request.Form["description"].FirstOrDefault(),
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
                _logger.LogError(ex, "Error creating dataset");
                return StatusCode(500, new { message = "An error occurred while uploading the file" });
            }
        }

        [HttpPost("{id}/upload")]
        [Authorize]
        public async Task<IActionResult> Upload(int id, CancellationToken ct)
        {
            try
            {
                if (!TryGetCurrentUserId(out var userId))
                    return Unauthorized(new { message = "Invalid user token" });

                var file = Request.Form.Files.FirstOrDefault();
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "No file provided" });

                var request = new ReplaceDatasetFileRequest(
                    File: file,
                    OriginalFileName: Request.Form["originalFileName"].FirstOrDefault(),
                    Description: Request.Form["description"].FirstOrDefault(),
                    UserId: userId);

                var result = await _service.ReplaceFileAsync(id, request, ct);
                if (result is null)
                    return NotFound(new { message = "Dataset not found" });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, new { message = "An error occurred while uploading the file" });
            }
        }

        [HttpGet("{id}/download")]
        [Authorize]
        public async Task<IActionResult> Download(int id, CancellationToken ct)
        {
            try
            {
                if (!TryGetCurrentUserId(out var userId))
                    return Unauthorized(new { message = "Invalid user token" });

                var dataset = await _service.GetByIdAsync(id, userId, ct);
                if (dataset == null)
                    return NotFound(new { message = "Dataset not found" });

                if (string.IsNullOrEmpty(dataset.StorageFileName))
                    return BadRequest(new { message = "No file associated with this dataset" });

                var filePath = _fileStorageService.GetStorageFilePath(dataset.UserId, dataset.StorageFileName);
                var fileStream = _fileStorageService.GetFileStream(filePath);
                return File(fileStream, "application/octet-stream", BuildDownloadFileName(dataset.OriginalFileName, dataset.FileExtension));
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { message = "File not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file");
                return StatusCode(500, new { message = "An error occurred while downloading the file" });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDatasetRequest request, CancellationToken ct)
        {
            try
            {
                if (!TryGetCurrentUserId(out var userId))
                    return Unauthorized(new { message = "Invalid user token" });

                var result = await _service.UpdateAsync(id, userId, request, ct);
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
                if (!TryGetCurrentUserId(out var userId))
                    return Unauthorized(new { message = "Invalid user token" });

                var dataset = await _service.GetByIdAsync(id, userId, ct);
                if (dataset == null)
                    return NotFound(new { message = "Dataset not found" });

                if (string.IsNullOrEmpty(dataset.StorageFileName))
                    return BadRequest(new { message = "No file associated with this dataset" });

                var filePath = _fileStorageService.GetStorageFilePath(dataset.UserId, dataset.StorageFileName);
                var previewRows = rows > 0 ? rows : DefaultPreviewRows;
                var previewPage = page > 0 ? page : 1;
                var previewData = _datasetPreviewService.ParseFilePreview(filePath, dataset.FileExtension, previewRows, previewPage);

                return Ok(new DatasetPreviewResponse(
                    dataset.OriginalFileName,
                    previewData.Columns,
                    previewData.Rows,
                    previewData.TotalRows,
                    previewPage,
                    previewRows,
                    true));
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { message = "File not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting preview");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            if (!TryGetCurrentUserId(out var userId))
                return Unauthorized(new { message = "Invalid user token" });

            var deleted = await _service.DeleteAsync(id, userId, ct);
            return deleted ? NoContent() : NotFound();
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out userId);
        }

        private static string BuildDownloadFileName(string datasetName, string fileExtension)
        {
            if (!string.IsNullOrWhiteSpace(fileExtension) &&
                !datasetName.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase))
            {
                return $"{datasetName}{fileExtension}";
            }

            return datasetName;
        }
    }
}
