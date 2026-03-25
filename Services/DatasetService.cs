using Microsoft.EntityFrameworkCore;
using RetailForecast.Data;
using RetailForecast.DTOs.Dataset;
using RetailForecast.Entities;

namespace RetailForecast.Services
{
    public class DatasetService
    {
        private readonly RetailForecastDbContext _context;
        private readonly FileStorageService _fileStorageService;
        private readonly ILogger<DatasetService> _logger;

        public DatasetService(
            RetailForecastDbContext context,
            FileStorageService fileStorageService,
            ILogger<DatasetService> logger)
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        public async Task<List<DatasetResponse>> GetAllAsync(int userId, CancellationToken ct = default)
        {
            var datasets = await _context.Datasets
                .AsNoTracking()
                .Where(d => d.UserId == userId)
                .ToListAsync(ct);

            return datasets.Select(MapResponse).ToList();
        }

        public async Task<DatasetResponse?> GetByIdAsync(int id, int userId, CancellationToken ct = default)
        {
            var dataset = await _context.Datasets
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, ct);

            return dataset is null ? null : MapResponse(dataset);
        }

        public async Task<DatasetResponse?> GetForTrainingRunAsync(int datasetId, int trainingRunId, CancellationToken ct = default)
        {
            var dataset = await _context.Datasets
                .AsNoTracking()
                .Where(d => d.Id == datasetId)
                .Where(d => d.TrainingRuns.Any(tr => tr.Id == trainingRunId))
                .FirstOrDefaultAsync(ct);

            return dataset is null ? null : MapResponse(dataset);
        }

        public async Task<DatasetResponse> CreateAsync(CreateDatasetRequest request, CancellationToken ct = default)
        {
            if (request.UserId <= 0)
                throw new ArgumentException("Valid UserId is required");

            var user = await _context.Users.FindAsync([request.UserId], ct);
            if (user is null)
                throw new InvalidOperationException("User not found");

            var datasetTitle = string.IsNullOrWhiteSpace(request.OriginalFileName)
                ? request.File?.FileName ?? "Unnamed Dataset"
                : request.OriginalFileName.Trim();

            var fileData = await SaveDatasetFileAsync(request.File, request.UserId);

            var dataset = new Dataset
            {
                OriginalFileName = datasetTitle,
                StorageFileName = fileData.StorageFileName,
                StorageFilePath = fileData.StorageFilePath,
                FileSizeBytes = fileData.FileSizeBytes,
                FileExtension = fileData.FileExtension,
                Description = NormalizeNullableText(request.Description),
                UserId = request.UserId
            };

            _context.Datasets.Add(dataset);
            await _context.SaveChangesAsync(ct);

            return MapResponse(dataset);
        }

        public async Task<DatasetResponse?> UpdateAsync(int id, int userId, UpdateDatasetRequest request, CancellationToken ct = default)
        {
            var dataset = await _context.Datasets
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, ct);

            if (dataset is null) return null;

            if (!string.IsNullOrWhiteSpace(request.OriginalFileName))
                dataset.OriginalFileName = request.OriginalFileName.Trim();

            if (request.Description != null)
                dataset.Description = NormalizeNullableText(request.Description);

            await _context.SaveChangesAsync(ct);

            return MapResponse(dataset);
        }

        public async Task<DatasetResponse?> ReplaceFileAsync(int id, ReplaceDatasetFileRequest request, CancellationToken ct = default)
        {
            var dataset = await _context.Datasets
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == request.UserId, ct);

            if (dataset is null) return null;

            var resolvedOriginalFileName = !string.IsNullOrWhiteSpace(request.OriginalFileName)
                ? request.OriginalFileName.Trim()
                : dataset.OriginalFileName;

            var previousStoragePath = dataset.StorageFilePath;
            var fileData = await SaveDatasetFileAsync(request.File, dataset.UserId);

            dataset.OriginalFileName = resolvedOriginalFileName;
            dataset.StorageFileName = fileData.StorageFileName;
            dataset.StorageFilePath = fileData.StorageFilePath;
            dataset.FileSizeBytes = fileData.FileSizeBytes;
            dataset.FileExtension = fileData.FileExtension;
            dataset.Description = NormalizeNullableText(request.Description);

            await _context.SaveChangesAsync(ct);

            if (!string.IsNullOrWhiteSpace(previousStoragePath) &&
                !string.Equals(previousStoragePath, dataset.StorageFilePath, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    _fileStorageService.DeleteFile(previousStoragePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to delete previous dataset file: {Message}", ex.Message);
                }
            }

            return MapResponse(dataset);
        }

        public async Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default)
        {
            var dataset = await _context.Datasets
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, ct);

            if (dataset is null) return false;

            if (!string.IsNullOrEmpty(dataset.StorageFilePath))
            {
                try
                {
                    _fileStorageService.DeleteFile(dataset.StorageFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to delete file: {Message}", ex.Message);
                }
            }

            _context.Datasets.Remove(dataset);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        private async Task<(string StorageFileName, string StorageFilePath, long FileSizeBytes, string FileExtension)> SaveDatasetFileAsync(IFormFile? file, int userId)
        {
            if (file == null || file.Length == 0)
            {
                return (string.Empty, string.Empty, 0, string.Empty);
            }

            var storageFileName = await _fileStorageService.SaveFileAsync(file, userId);
            var storageFilePath = _fileStorageService.GetStorageFilePath(userId, storageFileName);
            var fileSizeBytes = _fileStorageService.GetFileSizeBytes(storageFilePath);
            var fileExtension = Path.GetExtension(storageFileName).ToLowerInvariant();

            return (storageFileName, storageFilePath, fileSizeBytes, fileExtension);
        }

        private static string? NormalizeNullableText(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static DatasetResponse MapResponse(Dataset dataset)
            => new(
                dataset.Id,
                dataset.OriginalFileName,
                dataset.StorageFileName,
                dataset.FileSizeBytes,
                dataset.FileExtension,
                dataset.Description,
                dataset.CreatedAt,
                dataset.UpdatedAt,
                dataset.UserId);
    }
}
