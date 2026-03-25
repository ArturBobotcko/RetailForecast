using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailForecast.DTOs.TrainingRun;
using RetailForecast.Services;

namespace RetailForecast.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TrainingRunsController : ControllerBase
    {
        private readonly TrainingRunService _service;

        public TrainingRunsController(TrainingRunService service)
        {
            _service = service;
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
    }
}
