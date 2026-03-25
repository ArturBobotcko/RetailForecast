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

        public async Task<List<TrainingRunListResponse>> GetAllAsync(int userId, CancellationToken ct = default)
        {
            var trainingRuns = await _context.TrainingRuns
                .AsNoTracking()
                .Include(tr => tr.Dataset)
                .Include(tr => tr.Model)
                .Where(tr => tr.Dataset.UserId == userId)
                .OrderByDescending(tr => tr.CreatedAt)
                .ToListAsync(ct);

            return trainingRuns.Select(MapListResponse).ToList();
        }

        public async Task<TrainingRunDetailResponse?> GetByIdAsync(int id, int userId, CancellationToken ct = default)
        {
            var trainingRun = await _context.TrainingRuns
                .AsNoTracking()
                .Include(tr => tr.Dataset)
                .Include(tr => tr.Model)
                .Include(tr => tr.Features)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.Dataset.UserId == userId, ct);

            return trainingRun is null ? null : MapDetailResponse(trainingRun);
        }

        public async Task<TrainingRunDetailResponse> CreateAsync(
            CreateTrainingRunRequest request, int userId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.TargetColumn) || request.FeatureColumns is null || request.FeatureColumns.Count == 0)
                throw new ArgumentException("TargetColumn and at least one feature column are required");

            var dataset = await _context.Datasets
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == request.DatasetId && d.UserId == userId, ct);
            if (dataset is null)
                throw new InvalidOperationException("Dataset not found");

            var model = await _context.Models
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == request.ModelId, ct);
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

            var existing = await _context.Kpis
                .Where(k => k.DatasetId == request.DatasetId)
                .ToListAsync(ct);

            var missing = normalizedFeatureColumns
                .Where(column => existing.All(feature => !string.Equals(feature.Name, column, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (missing.Count > 0)
            {
                var createdFeatures = missing.Select(column => new Kpi
                {
                    Name = column,
                    DatasetId = request.DatasetId
                }).ToList();

                _context.Kpis.AddRange(createdFeatures);
                await _context.SaveChangesAsync(ct);
                existing.AddRange(createdFeatures);
            }

            var ordered = normalizedFeatureColumns
                .Select(column => existing.First(feature => string.Equals(feature.Name, column, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var trainingRun = new TrainingRun
            {
                TargetColumn = normalizedTargetColumn,
                DatasetId = request.DatasetId,
                ModelId = request.ModelId,
                StartedAt = DateTime.UtcNow,
                Status = TrainingStatus.Pending,
                Features = ordered
            };

            _context.TrainingRuns.Add(trainingRun);
            await _context.SaveChangesAsync(ct);

            return MapDetailResponse(trainingRun, dataset.OriginalFileName, model.Name, ordered);
        }

        public async Task<TrainingRunDetailResponse?> UpdateAsync(
            int id, int userId, UpdateTrainingRunRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.TargetColumn) && request.FinishedAt is null && string.IsNullOrWhiteSpace(request.Status))
                return null;

            var trainingRun = await _context.TrainingRuns
                .Include(tr => tr.Dataset)
                .Include(tr => tr.Model)
                .Include(tr => tr.Features)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.Dataset.UserId == userId, ct);

            if (trainingRun is null) return null;

            if (!string.IsNullOrWhiteSpace(request.TargetColumn))
                trainingRun.TargetColumn = request.TargetColumn.Trim();

            if (request.FinishedAt.HasValue)
                trainingRun.FinishedAt = request.FinishedAt;

            if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<TrainingStatus>(request.Status, out var status))
                trainingRun.Status = status;

            await _context.SaveChangesAsync(ct);

            return MapDetailResponse(trainingRun);
        }

        public async Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default)
        {
            var trainingRun = await _context.TrainingRuns
                .Include(tr => tr.Dataset)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.Dataset.UserId == userId, ct);

            if (trainingRun is null) return false;

            _context.TrainingRuns.Remove(trainingRun);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        private static TrainingRunListResponse MapListResponse(TrainingRun trainingRun)
            => new(
                trainingRun.Id,
                trainingRun.Status.ToString(),
                trainingRun.TargetColumn,
                trainingRun.DatasetId,
                trainingRun.Dataset.OriginalFileName,
                trainingRun.ModelId,
                trainingRun.Model.Name,
                trainingRun.StartedAt,
                trainingRun.FinishedAt,
                trainingRun.CreatedAt,
                trainingRun.UpdatedAt);

        private static TrainingRunDetailResponse MapDetailResponse(
            TrainingRun trainingRun,
            string? datasetName = null,
            string? modelName = null,
            IReadOnlyCollection<Kpi>? orderedFeatures = null)
        {
            var featureSource = orderedFeatures is not null
                ? orderedFeatures
                : trainingRun.Features.ToList();

            return new(
                trainingRun.Id,
                trainingRun.Status.ToString(),
                trainingRun.TargetColumn,
                trainingRun.DatasetId,
                datasetName ?? trainingRun.Dataset.OriginalFileName,
                trainingRun.ModelId,
                modelName ?? trainingRun.Model.Name,
                featureSource.Select(feature => feature.Name).ToList(),
                trainingRun.StartedAt,
                trainingRun.FinishedAt,
                trainingRun.CreatedAt,
                trainingRun.UpdatedAt);
        }
    }
}
