using Microsoft.EntityFrameworkCore;
using RetailForecast.Data;
using RetailForecast.DTOs.TrainingRun;
using RetailForecast.Entities;
using RetailForecast.Enums;

namespace RetailForecast.Services
{
    public class TrainingRunService
    {
        private readonly RetailForecastDbContext _context;

        public TrainingRunService(RetailForecastDbContext context)
        {
            _context = context;
        }

        public async Task<List<TrainingRunResponse>> GetAllAsync(CancellationToken ct = default)
        {
            var trainingRuns = await _context.TrainingRuns
                .AsNoTracking()
                .Include(tr => tr.Dataset)
                .Include(tr => tr.Model)
                .Include(tr => tr.Features)
                .ToListAsync(ct);

            return trainingRuns.Select(MapResponse).ToList();
        }

        public async Task<TrainingRunResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var trainingRun = await _context.TrainingRuns
                .AsNoTracking()
                .Include(tr => tr.Dataset)
                .Include(tr => tr.Model)
                .Include(tr => tr.Features)
                .FirstOrDefaultAsync(tr => tr.Id == id, ct);

            return trainingRun is null ? null : MapResponse(trainingRun);
        }

        public async Task<TrainingRunResponse?> CreateAsync(
            CreateTrainingRunRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.TargetColumn) || request.FeatureColumns is null || request.FeatureColumns.Count == 0)
                throw new ArgumentException("TargetColumn and at least one feature column are required");

            var dataset = await _context.Datasets.FindAsync([request.DatasetId], ct);
            if (dataset is null)
                throw new InvalidOperationException("Dataset not found");

            var model = await _context.Models.FindAsync([request.ModelId], ct);
            if (model is null)
                throw new InvalidOperationException("Model not found");

            var normalizedFeatureColumns = request.FeatureColumns
                .Where(column => !string.IsNullOrWhiteSpace(column))
                .Select(column => column.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalizedFeatureColumns.Count == 0)
                throw new ArgumentException("At least one feature column is required");

            var normalizedTargetColumn = request.TargetColumn.Trim();
            if (normalizedFeatureColumns.Any(column => string.Equals(column, normalizedTargetColumn, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Target column cannot be included in feature columns");

            var existingFeatures = await _context.Kpis
                .Where(k => k.DatasetId == request.DatasetId && normalizedFeatureColumns.Contains(k.Name))
                .ToListAsync(ct);

            var missingFeatureColumns = normalizedFeatureColumns
                .Where(column => existingFeatures.All(feature => !string.Equals(feature.Name, column, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (missingFeatureColumns.Count > 0)
            {
                var newFeatures = missingFeatureColumns.Select(column => new Kpi
                {
                    Name = column,
                    DataType = "Unknown",
                    DatasetId = request.DatasetId
                }).ToList();

                _context.Kpis.AddRange(newFeatures);
                await _context.SaveChangesAsync(ct);
                existingFeatures.AddRange(newFeatures);
            }

            var orderedFeatures = normalizedFeatureColumns
                .Select(column => existingFeatures.First(feature => string.Equals(feature.Name, column, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var trainingRun = new TrainingRun
            {
                TargetColumn = normalizedTargetColumn,
                DatasetId = request.DatasetId,
                ModelId = request.ModelId,
                StartedAt = DateTime.UtcNow,
                Status = TrainingStatus.Pending,
                Features = orderedFeatures
            };

            _context.TrainingRuns.Add(trainingRun);
            await _context.SaveChangesAsync(ct);

            return new TrainingRunResponse(
                trainingRun.Id,
                trainingRun.TargetColumn,
                trainingRun.StartedAt,
                trainingRun.FinishedAt,
                trainingRun.Status.ToString(),
                dataset.Id,
                dataset.OriginalFileName,
                model.Id,
                model.Name,
                orderedFeatures.Select(feature => feature.Name).ToList(),
                trainingRun.CreatedAt,
                trainingRun.UpdatedAt);
        }

        public async Task<TrainingRunResponse?> UpdateAsync(
            int id, UpdateTrainingRunRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.TargetColumn) && request.FinishedAt is null && string.IsNullOrWhiteSpace(request.Status))
                return null;

            var trainingRun = await _context.TrainingRuns
                .Include(tr => tr.Dataset)
                .Include(tr => tr.Model)
                .Include(tr => tr.Features)
                .FirstOrDefaultAsync(tr => tr.Id == id, ct);

            if (trainingRun is null) return null;

            if (!string.IsNullOrWhiteSpace(request.TargetColumn))
                trainingRun.TargetColumn = request.TargetColumn;

            if (request.FinishedAt.HasValue)
                trainingRun.FinishedAt = request.FinishedAt;

            if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<TrainingStatus>(request.Status, out var status))
                trainingRun.Status = status;

            await _context.SaveChangesAsync(ct);

            return MapResponse(trainingRun);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var trainingRun = await _context.TrainingRuns.FindAsync([id], ct);

            if (trainingRun is null) return false;

            _context.TrainingRuns.Remove(trainingRun);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        private static TrainingRunResponse MapResponse(TrainingRun trainingRun)
            => new(
                trainingRun.Id,
                trainingRun.TargetColumn,
                trainingRun.StartedAt,
                trainingRun.FinishedAt,
                trainingRun.Status.ToString(),
                trainingRun.DatasetId,
                trainingRun.Dataset.OriginalFileName,
                trainingRun.ModelId,
                trainingRun.Model.Name,
                trainingRun.Features.Select(feature => feature.Name).ToList(),
                trainingRun.CreatedAt,
                trainingRun.UpdatedAt);
    }
}
