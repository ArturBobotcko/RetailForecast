using Microsoft.EntityFrameworkCore;
using RetailForecast.Data;
using RetailForecast.DTOs.Forecast;
using RetailForecast.DTOs.TrainingMetric;
using RetailForecast.DTOs.TrainingRun;
using RetailForecast.Entities;
using RetailForecast.Enums;

namespace RetailForecast.Services
{
    public class TrainingRunService
    {
        private readonly RetailForecastDbContext _context;
        private readonly MlServiceClient _mlServiceClient;

        public TrainingRunService(RetailForecastDbContext context, MlServiceClient mlServiceClient)
        {
            _context = context;
            _mlServiceClient = mlServiceClient;
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
                .Include(tr => tr.Metrics)
                .Include(tr => tr.Forecasts)
                    .ThenInclude(f => f.ForecastValues)
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
                .Include(tr => tr.Metrics)
                .Include(tr => tr.Forecasts)
                    .ThenInclude(f => f.ForecastValues)
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
                trainingRun.ExternalJobId,
                trainingRun.StartedAt,
                trainingRun.FinishedAt,
                trainingRun.CreatedAt,
                trainingRun.UpdatedAt);

        public async Task<StartTrainingRunResponse?> StartAsync(
            int id,
            int userId,
            string downloadUrl,
            string callbackUrl,
            CancellationToken ct = default)
        {
            var trainingRun = await _context.TrainingRuns
                .Include(tr => tr.Dataset)
                .Include(tr => tr.Model)
                .Include(tr => tr.Features)
                .FirstOrDefaultAsync(tr => tr.Id == id && tr.Dataset.UserId == userId, ct);

            if (trainingRun is null)
                return null;

            if (trainingRun.Status != TrainingStatus.Pending)
                throw new InvalidOperationException("Only pending training runs can be started");

            var request = new MlTrainingStartRequest(
                trainingRun.Id,
                trainingRun.DatasetId,
                downloadUrl.Replace("{datasetId}", trainingRun.DatasetId.ToString(), StringComparison.Ordinal),
                callbackUrl,
                trainingRun.TargetColumn,
                trainingRun.Features.Select(feature => feature.Name).ToList(),
                new MlTrainingModelDto(
                    trainingRun.ModelId,
                    trainingRun.Model.Name,
                    trainingRun.Model.Algorithm));

            var response = await _mlServiceClient.StartTrainingAsync(request, ct);

            trainingRun.Status = TrainingStatus.Running;
            trainingRun.StartedAt = DateTime.UtcNow;
            trainingRun.FinishedAt = null;
            trainingRun.ErrorMessage = null;
            trainingRun.ExternalJobId = string.IsNullOrWhiteSpace(response?.ExternalJobId)
                ? trainingRun.ExternalJobId
                : response!.ExternalJobId!.Trim();

            await _context.SaveChangesAsync(ct);

            return new StartTrainingRunResponse(
                trainingRun.Id,
                trainingRun.Status.ToString(),
                trainingRun.ExternalJobId);
        }

        public async Task<TrainingRunDetailResponse?> ApplyCallbackAsync(
            int id,
            TrainingRunCallbackRequest request,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Status))
                throw new ArgumentException("Status is required");

            if (!Enum.TryParse<TrainingStatus>(request.Status, true, out var status))
                throw new ArgumentException("Invalid training status");

            var trainingRun = await _context.TrainingRuns
                .Include(tr => tr.Dataset)
                .Include(tr => tr.Model)
                .Include(tr => tr.Features)
                .Include(tr => tr.Metrics)
                .Include(tr => tr.Forecasts)
                    .ThenInclude(f => f.ForecastValues)
                .FirstOrDefaultAsync(tr => tr.Id == id, ct);

            if (trainingRun is null)
                return null;

            trainingRun.Status = status;
            trainingRun.ErrorMessage = string.IsNullOrWhiteSpace(request.Error) ? null : request.Error.Trim();

            if (!string.IsNullOrWhiteSpace(request.ExternalJobId))
                trainingRun.ExternalJobId = request.ExternalJobId.Trim();

            if (status is TrainingStatus.Completed or TrainingStatus.Failed)
                trainingRun.FinishedAt = DateTime.UtcNow;
            else
                trainingRun.FinishedAt = null;

            _context.TrainingMetrics.RemoveRange(trainingRun.Metrics);
            trainingRun.Metrics.Clear();

            var existingForecasts = trainingRun.Forecasts.ToList();
            if (existingForecasts.Count > 0)
            {
                _context.ForecastValues.RemoveRange(existingForecasts.SelectMany(forecast => forecast.ForecastValues));
                _context.Forecasts.RemoveRange(existingForecasts);
                trainingRun.Forecasts.Clear();
            }

            if (request.Metrics is not null)
            {
                var metrics = request.Metrics
                    .Where(metric => !string.IsNullOrWhiteSpace(metric.Name))
                    .Select(metric => new TrainingMetric
                    {
                        Name = metric.Name.Trim(),
                        Value = metric.Value,
                        TrainingRunId = trainingRun.Id
                    })
                    .ToList();

                if (metrics.Count > 0)
                {
                    _context.TrainingMetrics.AddRange(metrics);
                    foreach (var metric in metrics)
                    {
                        trainingRun.Metrics.Add(metric);
                    }
                }
            }

            if (request.Forecast is not null && request.Forecast.Count > 0)
            {
                var forecast = new Forecast
                {
                    Horizon = request.Forecast.Count,
                    TrainingRunId = trainingRun.Id
                };

                foreach (var value in request.Forecast.OrderBy(value => value.Timestamp))
                {
                    forecast.ForecastValues.Add(new ForecastValue
                    {
                        Timestamp = value.Timestamp.Kind == DateTimeKind.Utc
                            ? value.Timestamp
                            : DateTime.SpecifyKind(value.Timestamp, DateTimeKind.Utc),
                        Value = value.Value
                    });
                }

                _context.Forecasts.Add(forecast);
                trainingRun.Forecasts.Add(forecast);
            }

            await _context.SaveChangesAsync(ct);

            return MapDetailResponse(trainingRun);
        }

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
                trainingRun.ExternalJobId,
                trainingRun.ErrorMessage,
                featureSource.Select(feature => feature.Name).ToList(),
                trainingRun.Metrics.Select(MapMetricResponse).ToList(),
                trainingRun.Forecasts
                    .SelectMany(forecast => forecast.ForecastValues)
                    .OrderBy(value => value.Timestamp)
                    .Select(MapForecastValueResponse)
                    .ToList(),
                trainingRun.StartedAt,
                trainingRun.FinishedAt,
                trainingRun.CreatedAt,
                trainingRun.UpdatedAt);
        }

        private static TrainingMetricResponse MapMetricResponse(TrainingMetric metric)
            => new(
                metric.Id,
                metric.Name,
                metric.Value,
                metric.TrainingRunId,
                metric.CreatedAt,
                metric.UpdatedAt);

        private static ForecastValueResponse MapForecastValueResponse(ForecastValue value)
            => new(
                value.Timestamp,
                value.Value);
    }
}
