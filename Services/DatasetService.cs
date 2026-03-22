using Microsoft.EntityFrameworkCore;
using RetailForecast.DTOs.Dataset;
using RetailForecast.Entities;
using RetailForecast.Data;
using System;

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

        public async Task<List<DatasetResponse>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Datasets
                .AsNoTracking()
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

        public async Task<DatasetResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var dataset = await _context.Datasets
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id, ct);

            if (dataset is null) return null;

            return new DatasetResponse(
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

            // If file is provided, save it
            if (request.File != null && request.File.Length > 0)
            {
                var originalFileName = request.OriginalFileName ?? request.File.FileName;
                storageFileName = await _fileStorageService.SaveFileAsync(request.File, request.UserId, originalFileName);
                storageFilePath = _fileStorageService.GetStorageFilePath(request.UserId, storageFileName);
                fileSizeBytes = _fileStorageService.GetFileSizeBytes(storageFilePath);
                fileExtension = Path.GetExtension(storageFileName).ToLower();
            }

            var dataset = new Dataset
            {
                OriginalFileName = request.OriginalFileName ?? request.File?.FileName ?? "Unnamed Dataset",
                StorageFileName = storageFileName,
                StorageFilePath = storageFilePath,
                FileSizeBytes = fileSizeBytes,
                FileExtension = fileExtension,
                Description = request.Description,
                UserId = request.UserId
            };

            _context.Datasets.Add(dataset);
            await _context.SaveChangesAsync(ct);

            return new DatasetResponse(
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

        public async Task<DatasetResponse?> UpdateAsync(
            int id, UpdateDatasetRequest request, CancellationToken ct = default)
        {
            var dataset = await _context.Datasets.FindAsync([id], ct);

            if (dataset is null) return null;

            if (!string.IsNullOrWhiteSpace(request.OriginalFileName))
                dataset.OriginalFileName = request.OriginalFileName;

            await _context.SaveChangesAsync(ct);

            return new DatasetResponse(
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

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var dataset = await _context.Datasets.FindAsync([id], ct);

            if (dataset is null) return false;

            // Delete file from storage first
            if (!string.IsNullOrEmpty(dataset.StorageFilePath))
            {
                try
                {
                    _fileStorageService.DeleteFile(dataset.StorageFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to delete file: {ex.Message}");
                    // Continue with DB deletion even if file deletion fails
                }
            }

            _context.Datasets.Remove(dataset);
            await _context.SaveChangesAsync(ct);

            return true;
        }
    }
}