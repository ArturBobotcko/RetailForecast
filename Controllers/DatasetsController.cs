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

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var deleted = await _service.DeleteAsync(id, ct);
            return deleted ? NoContent() : NotFound();
        }
    }
}