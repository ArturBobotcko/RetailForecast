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
            return await _context.TrainingRuns
                .AsNoTracking()
                .Select(tr => new TrainingRunResponse(
                    tr.Id,
                    tr.TargetColumn,
                    tr.StartedAt,
                    tr.FinishedAt,
                    tr.Status.ToString(),
                    tr.DatasetId,
                    tr.ModelId,
                    tr.CreatedAt))
                .ToListAsync(ct);
        }

        public async Task<TrainingRunResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var trainingRun = await _context.TrainingRuns
                .AsNoTracking()
                .FirstOrDefaultAsync(tr => tr.Id == id, ct);

            if (trainingRun is null) return null;

            return new TrainingRunResponse(
                trainingRun.Id,
                trainingRun.TargetColumn,
                trainingRun.StartedAt,
                trainingRun.FinishedAt,
                trainingRun.Status.ToString(),
                trainingRun.DatasetId,
                trainingRun.ModelId,
                trainingRun.CreatedAt);
        }

        public async Task<TrainingRunResponse?> CreateAsync(
            CreateTrainingRunRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.TargetColumn) || request.FeatureIds.Count == 0)
                throw new ArgumentException("TargetColumn and at least one FeatureId are required");

            var dataset = await _context.Datasets.FindAsync([request.DatasetId], ct);
            if (dataset is null)
                throw new InvalidOperationException("Dataset not found");

            var model = await _context.Models.FindAsync([request.ModelId], ct);
            if (model is null)
                throw new InvalidOperationException("Model not found");

            var features = await _context.Kpis
                .Where(k => request.FeatureIds.Contains(k.Id) && k.DatasetId == request.DatasetId)
                .ToListAsync(ct);

            if (features.Count != request.FeatureIds.Count)
                throw new InvalidOperationException("Some features not found or belong to different dataset");

            var trainingRun = new TrainingRun
            {
                TargetColumn = request.TargetColumn,
                DatasetId = request.DatasetId,
                ModelId = request.ModelId,
                StartedAt = DateTime.UtcNow,
                Status = TrainingStatus.Pending,
                Features = features
            };

            _context.TrainingRuns.Add(trainingRun);
            await _context.SaveChangesAsync(ct);

            return new TrainingRunResponse(
                trainingRun.Id,
                trainingRun.TargetColumn,
                trainingRun.StartedAt,
                trainingRun.FinishedAt,
                trainingRun.Status.ToString(),
                trainingRun.DatasetId,
                trainingRun.ModelId,
                trainingRun.CreatedAt);
        }

        public async Task<TrainingRunResponse?> UpdateAsync(
            int id, UpdateTrainingRunRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.TargetColumn) && request.FinishedAt is null && string.IsNullOrWhiteSpace(request.Status))
                return null;

            var trainingRun = await _context.TrainingRuns.FindAsync([id], ct);

            if (trainingRun is null) return null;

            if (!string.IsNullOrWhiteSpace(request.TargetColumn))
                trainingRun.TargetColumn = request.TargetColumn;

            if (request.FinishedAt.HasValue)
                trainingRun.FinishedAt = request.FinishedAt;

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (Enum.TryParse<TrainingStatus>(request.Status, out var status))
                {
                    trainingRun.Status = status;
                }
            }

            await _context.SaveChangesAsync(ct);

            return new TrainingRunResponse(
                trainingRun.Id,
                trainingRun.TargetColumn,
                trainingRun.StartedAt,
                trainingRun.FinishedAt,
                trainingRun.Status.ToString(),
                trainingRun.DatasetId,
                trainingRun.ModelId,
                trainingRun.CreatedAt);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var trainingRun = await _context.TrainingRuns.FindAsync([id], ct);

            if (trainingRun is null) return false;

            _context.TrainingRuns.Remove(trainingRun);
            await _context.SaveChangesAsync(ct);

            return true;
        }
    }
}
