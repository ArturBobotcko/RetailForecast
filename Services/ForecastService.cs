using Microsoft.EntityFrameworkCore;
using RetailForecast.Data;
using RetailForecast.DTOs.Forecast;
using RetailForecast.Entities;
using RetailForecast.Enums;

namespace RetailForecast.Services
{
    public class ForecastService
    {
        private readonly RetailForecastDbContext _context;

        public ForecastService(RetailForecastDbContext context)
        {
            _context = context;
        }

        public async Task<List<ForecastResponse>> GetAllAsync(int userId, CancellationToken ct = default)
        {
            var forecasts = await _context.Forecasts
                .AsNoTracking()
                .Include(f => f.TrainingRun)
                    .ThenInclude(tr => tr.Dataset)
                .Include(f => f.TrainingRun)
                    .ThenInclude(tr => tr.Model)
                .Include(f => f.ForecastValues)
                .Where(f => f.TrainingRun.Dataset.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync(ct);

            return forecasts.Select(MapResponse).ToList();
        }

        public async Task<ForecastResponse?> GetByIdAsync(int id, int userId, CancellationToken ct = default)
        {
            var forecast = await _context.Forecasts
                .AsNoTracking()
                .Include(f => f.TrainingRun)
                    .ThenInclude(tr => tr.Dataset)
                .Include(f => f.TrainingRun)
                    .ThenInclude(tr => tr.Model)
                .Include(f => f.ForecastValues)
                .FirstOrDefaultAsync(f => f.Id == id && f.TrainingRun.Dataset.UserId == userId, ct);

            if (forecast is null) return null;

            return MapResponse(forecast);
        }

        public async Task<ForecastResponse?> CreateAsync(
            CreateForecastRequest request, CancellationToken ct = default)
        {
            if (request.Horizon <= 0)
                throw new ArgumentException("Horizon must be greater than 0");

            var trainingRun = await _context.TrainingRuns.FindAsync([request.TrainingRunId], ct);
            if (trainingRun is null)
                throw new InvalidOperationException("TrainingRun not found");

            var forecast = new Forecast
            {
                Horizon = request.Horizon,
                TrainingRunId = request.TrainingRunId
            };

            _context.Forecasts.Add(forecast);
            await _context.SaveChangesAsync(ct);

            var createdForecast = await _context.Forecasts
                .AsNoTracking()
                .Include(f => f.TrainingRun)
                    .ThenInclude(tr => tr.Dataset)
                .Include(f => f.TrainingRun)
                    .ThenInclude(tr => tr.Model)
                .Include(f => f.ForecastValues)
                .FirstOrDefaultAsync(f => f.Id == forecast.Id, ct);

            return createdForecast is null ? null : MapResponse(createdForecast);
        }

        public async Task<ForecastResponse?> UpdateAsync(
            int id, UpdateForecastRequest request, CancellationToken ct = default)
        {
            if (!request.Horizon.HasValue)
                return null;

            if (request.Horizon <= 0)
                return null;

            var forecast = await _context.Forecasts.FindAsync([id], ct);

            if (forecast is null) return null;

            forecast.Horizon = request.Horizon.Value;

            await _context.SaveChangesAsync(ct);

            var updatedForecast = await _context.Forecasts
                .AsNoTracking()
                .Include(f => f.TrainingRun)
                    .ThenInclude(tr => tr.Dataset)
                .Include(f => f.TrainingRun)
                    .ThenInclude(tr => tr.Model)
                .Include(f => f.ForecastValues)
                .FirstOrDefaultAsync(f => f.Id == forecast.Id, ct);

            return updatedForecast is null ? null : MapResponse(updatedForecast);
        }

        public async Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default)
        {
            var forecast = await _context.Forecasts
                .Include(f => f.TrainingRun)
                .FirstOrDefaultAsync(f => f.Id == id && f.TrainingRun.Dataset.UserId == userId, ct);

            if (forecast is null) return false;

            _context.Forecasts.Remove(forecast);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        private static ForecastResponse MapResponse(Forecast forecast)
            => new(
                forecast.Id,
                forecast.Horizon,
                forecast.TrainingRunId,
                forecast.TrainingRun.Dataset.OriginalFileName,
                forecast.TrainingRun.Model.Name,
                forecast.TrainingRun.TargetColumn,
                forecast.TrainingRun.Status.ToString(),
                forecast.ForecastValues
                    .OrderBy(value => value.Timestamp)
                    .Select(value => new ForecastValueResponse(value.Timestamp, value.Value))
                    .ToList(),
                forecast.CreatedAt,
                forecast.UpdatedAt);
    }
}
