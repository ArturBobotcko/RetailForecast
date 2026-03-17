using Microsoft.EntityFrameworkCore;
using RetailForecast.Data;
using RetailForecast.DTOs.TrainingMetric;
using RetailForecast.Entities;

namespace RetailForecast.Services
{
    public class TrainingMetricService
    {
        private readonly RetailForecastDbContext _context;

        public TrainingMetricService(RetailForecastDbContext context)
        {
            _context = context;
        }

        public async Task<List<TrainingMetricResponse>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.TrainingMetrics
                .AsNoTracking()
                .Select(tm => new TrainingMetricResponse(
                    tm.Id,
                    tm.Name,
                    tm.Value,
                    tm.TrainingRunId,
                    tm.CreatedAt))
                .ToListAsync(ct);
        }

        public async Task<TrainingMetricResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var metric = await _context.TrainingMetrics.FindAsync([id], ct);

            if (metric is null) return null;

            return new TrainingMetricResponse(
                metric.Id,
                metric.Name,
                metric.Value,
                metric.TrainingRunId,
                metric.CreatedAt);
        }

        public async Task<TrainingMetricResponse?> CreateAsync(
            CreateTrainingMetricRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Name is required");
            
            if (request.Value < 0)
                throw new ArgumentException("Value cannot be negative");

            var trainingRun = await _context.TrainingRuns.FindAsync([request.TrainingRunId], ct);
            if (trainingRun is null)
                throw new InvalidOperationException("TrainingRun not found");

            var metric = new TrainingMetric
            {
                Name = request.Name,
                Value = request.Value,
                TrainingRunId = request.TrainingRunId
            };

            _context.TrainingMetrics.Add(metric);
            await _context.SaveChangesAsync(ct);

            return new TrainingMetricResponse(
                metric.Id,
                metric.Name,
                metric.Value,
                metric.TrainingRunId,
                metric.CreatedAt);
        }

        public async Task<TrainingMetricResponse?> UpdateAsync(
            int id, UpdateTrainingMetricRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name) && !request.Value.HasValue)
                return null;

            var metric = await _context.TrainingMetrics.FindAsync([id], ct);

            if (metric is null) return null;

            if (!string.IsNullOrWhiteSpace(request.Name))
                metric.Name = request.Name;

            if (request.Value.HasValue)
            {
                if (request.Value < 0)
                    return null;
                metric.Value = request.Value.Value;
            }

            await _context.SaveChangesAsync(ct);

            return new TrainingMetricResponse(
                metric.Id,
                metric.Name,
                metric.Value,
                metric.TrainingRunId,
                metric.CreatedAt);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var metric = await _context.TrainingMetrics.FindAsync([id], ct);

            if (metric is null) return false;

            _context.TrainingMetrics.Remove(metric);
            await _context.SaveChangesAsync(ct);

            return true;
        }
    }
}
