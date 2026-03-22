using Microsoft.EntityFrameworkCore;
using RetailForecast.DTOs.Dataset;
using RetailForecast.Entities;
using RetailForecast.Data;

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
            return await _context.Datasets
                .AsNoTracking()
                .Where(d => d.UserId == userId)
                .Select(d => new DatasetResponse(
                    d.Id,
                    d.OriginalFileName,
                    d.StorageFileName,
                    d.FileSizeBytes,
                    d.FileExtension,
                    d.Description,
                    d.CreatedAt,
                    d.UpdatedAt,
                    d.UserId))
                .ToListAsync(ct);
        }

        public async Task<DatasetResponse?> GetByIdAsync(int id, int userId, CancellationToken ct = default)
        {
            var dataset = await _context.Datasets
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, ct);

            if (dataset is null) return null;

            return MapResponse(dataset);
        }

        public async Task<DatasetResponse> CreateAsync(
            CreateDatasetRequest request, CancellationToken ct = default)
        {
            if (request.UserId <= 0)
                throw new ArgumentException("Valid UserId is required");

            var user = await _context.Users.FindAsync([request.UserId], ct);
            if (user is null)
                throw new InvalidOperationException("User not found");

            string storageFileName = string.Empty;
            string storageFilePath = string.Empty;
            long fileSizeBytes = 0;
            string fileExtension = string.Empty;
            var datasetTitle = string.IsNullOrWhiteSpace(request.OriginalFileName)
                ? request.File?.FileName ?? "Unnamed Dataset"
                : request.OriginalFileName.Trim();

            if (request.File != null && request.File.Length > 0)
            {
                storageFileName = await _fileStorageService.SaveFileAsync(request.File, request.UserId);
                storageFilePath = _fileStorageService.GetStorageFilePath(request.UserId, storageFileName);
                fileSizeBytes = _fileStorageService.GetFileSizeBytes(storageFilePath);
                fileExtension = Path.GetExtension(storageFileName).ToLowerInvariant();
            }

            var dataset = new Dataset
            {
                OriginalFileName = datasetTitle,
                StorageFileName = storageFileName,
                StorageFilePath = storageFilePath,
                FileSizeBytes = fileSizeBytes,
                FileExtension = fileExtension,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                UserId = request.UserId
            };

            _context.Datasets.Add(dataset);
            await _context.SaveChangesAsync(ct);

            return MapResponse(dataset);
        }

        public async Task<DatasetResponse?> UpdateAsync(
            int id, int userId, UpdateDatasetRequest request, CancellationToken ct = default)
        {
            var dataset = await _context.Datasets.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, ct);

            if (dataset is null) return null;

            if (request.OriginalFileName != null && !string.IsNullOrWhiteSpace(request.OriginalFileName))
                dataset.OriginalFileName = request.OriginalFileName.Trim();

            if (request.Description != null)
                dataset.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

            await _context.SaveChangesAsync(ct);

            return MapResponse(dataset);
        }

        public async Task<DatasetResponse?> ReplaceFileAsync(
            int id,
            int userId,
            IFormFile file,
            string? originalFileName,
            string? description,
            CancellationToken ct = default)
        {
            var dataset = await _context.Datasets.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, ct);
            if (dataset is null) return null;

            var resolvedOriginalFileName = !string.IsNullOrWhiteSpace(originalFileName)
                ? originalFileName.Trim()
                : dataset.OriginalFileName;

            var storageFileName = await _fileStorageService.SaveFileAsync(file, dataset.UserId);
            var storageFilePath = _fileStorageService.GetStorageFilePath(dataset.UserId, storageFileName);
            var fileSizeBytes = _fileStorageService.GetFileSizeBytes(storageFilePath);
            var previousStoragePath = dataset.StorageFilePath;

            dataset.OriginalFileName = resolvedOriginalFileName;
            dataset.StorageFileName = storageFileName;
            dataset.StorageFilePath = storageFilePath;
            dataset.FileSizeBytes = fileSizeBytes;
            dataset.FileExtension = Path.GetExtension(storageFileName).ToLowerInvariant();
            dataset.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

            await _context.SaveChangesAsync(ct);

            if (!string.IsNullOrWhiteSpace(previousStoragePath) &&
                !string.Equals(previousStoragePath, storageFilePath, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    _fileStorageService.DeleteFile(previousStoragePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to delete previous dataset file: {ex.Message}");
                }
            }

            return MapResponse(dataset);
        }

        public async Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default)
        {
            var dataset = await _context.Datasets.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, ct);

            if (dataset is null) return false;

            if (!string.IsNullOrEmpty(dataset.StorageFilePath))
            {
                try
                {
                    _fileStorageService.DeleteFile(dataset.StorageFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to delete file: {ex.Message}");
                }
            }

            _context.Datasets.Remove(dataset);
            await _context.SaveChangesAsync(ct);

            return true;
        }

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
