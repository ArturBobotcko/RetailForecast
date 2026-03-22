using Microsoft.EntityFrameworkCore;
using RetailForecast.Data;
using RetailForecast.DTOs.Forecast;
using RetailForecast.Entities;

namespace RetailForecast.Services
{
    public class ForecastService
    {
        private readonly RetailForecastDbContext _context;

        public ForecastService(RetailForecastDbContext context)
        {
            _context = context;
        }

        public async Task<List<ForecastResponse>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Forecasts
                .AsNoTracking()
                .Select(f => new ForecastResponse(
                    f.Id,
                    f.Horizon,
                    f.TrainingRunId,
                    f.CreatedAt,
                    f.UpdatedAt))
                .ToListAsync(ct);
        }

        public async Task<ForecastResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var forecast = await _context.Forecasts.FindAsync([id], ct);

            if (forecast is null) return null;

            return new ForecastResponse(
                forecast.Id,
                forecast.Horizon,
                forecast.TrainingRunId,
                forecast.CreatedAt,
                forecast.UpdatedAt);
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

            return new ForecastResponse(
                forecast.Id,
                forecast.Horizon,
                forecast.TrainingRunId,
                forecast.CreatedAt,
                forecast.UpdatedAt);
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

            return new ForecastResponse(
                forecast.Id,
                forecast.Horizon,
                forecast.TrainingRunId,
                forecast.CreatedAt,
                forecast.UpdatedAt);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var forecast = await _context.Forecasts.FindAsync([id], ct);

            if (forecast is null) return false;

            _context.Forecasts.Remove(forecast);
            await _context.SaveChangesAsync(ct);

            return true;
        }
    }
}
