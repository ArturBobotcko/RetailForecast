using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailForecast.DTOs.TrainingRun;
using RetailForecast.Services;
using RetailForecast.Settings;

namespace RetailForecast.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TrainingRunsController : ControllerBase
    {
        private readonly TrainingRunService _service;
        private readonly ApplicationSettings _applicationSettings;

        public TrainingRunsController(
            TrainingRunService service,
            Microsoft.Extensions.Options.IOptions<ApplicationSettings> applicationSettings)
        {
            _service = service;
            _applicationSettings = applicationSettings.Value;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            if (!TryGetCurrentUserId(out var userId))
                return Unauthorized(new { message = "Invalid user token" });

            return Ok(await _service.GetAllAsync(userId, ct));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            if (!TryGetCurrentUserId(out var userId))
                return Unauthorized(new { message = "Invalid user token" });

            var result = await _service.GetByIdAsync(id, userId, ct);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateTrainingRunRequest request, CancellationToken ct)
        {
            try
            {
                if (!TryGetCurrentUserId(out var userId))
                    return Unauthorized(new { message = "Invalid user token" });

                var result = await _service.CreateAsync(request, userId, ct);
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
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id, [FromBody] UpdateTrainingRunRequest request, CancellationToken ct)
        {
            try
            {
                if (!TryGetCurrentUserId(out var userId))
                    return Unauthorized(new { message = "Invalid user token" });

                var result = await _service.UpdateAsync(id, userId, request, ct);
                if (result is null)
                    return NotFound(new { message = "TrainingRun not found or no fields to update" });
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/start")]
        public async Task<IActionResult> Start(int id, CancellationToken ct)
        {
            try
            {
                if (!TryGetCurrentUserId(out var userId))
                    return Unauthorized(new { message = "Invalid user token" });

                var baseUrl = ResolvePublicBaseUrl();
                var downloadUrl = $"{baseUrl}/api/datasets/internal/{{datasetId}}/download?trainingRunId={id}";
                var callbackUrl = $"{baseUrl}/api/trainingruns/internal/{id}/result";

                var result = await _service.StartAsync(
                    id,
                    userId,
                    downloadUrl,
                    callbackUrl,
                    ct);

                return result is null
                    ? NotFound(new { message = "TrainingRun not found" })
                    : Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { message = ex.Message });
            }
        }

        [HttpPost("internal/{id}/result")]
        [AllowAnonymous]
        public async Task<IActionResult> ApplyResult(int id, [FromBody] TrainingRunCallbackRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _service.ApplyCallbackAsync(id, request, ct);
                return result is null
                    ? NotFound(new { message = "TrainingRun not found" })
                    : Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
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

        private string ResolvePublicBaseUrl()
        {
            if (!string.IsNullOrWhiteSpace(_applicationSettings.PublicBaseUrl))
                return _applicationSettings.PublicBaseUrl.TrimEnd('/');

            return $"{Request.Scheme}://{Request.Host}";
        }
    }
}
