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

        public DatasetService(RetailForecastDbContext context)
        {
            _context = context;
        }

        public async Task<List<DatasetResponse>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Datasets
                .AsNoTracking()
                .Select(d => new DatasetResponse(
                    d.Id,
                    d.OriginalFileName,
                    d.CreatedAt,
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
                dataset.CreatedAt,
                dataset.UserId);
        }

        public async Task<DatasetResponse> CreateAsync(
            CreateDatasetRequest request, CancellationToken ct = default)
        {
            var dataset = new Dataset
            {
                OriginalFileName = request.OriginalFileName,
                UserId = request.UserId,
                StorageFilePath = "temp"
            };

            _context.Datasets.Add(dataset);
            await _context.SaveChangesAsync(ct);

            return new DatasetResponse(
                dataset.Id,
                dataset.OriginalFileName,
                dataset.CreatedAt,
                dataset.UserId);
        }

        public async Task<DatasetResponse?> UpdateAsync(
            int id, UpdateDatasetRequest request, CancellationToken ct = default)
        {
            var dataset = await _context.Datasets.FindAsync([id], ct);

            if (dataset is null) return null;

            dataset.OriginalFileName = request.OriginalFileName;
            await _context.SaveChangesAsync(ct);

            return new DatasetResponse(
                dataset.Id,
                dataset.OriginalFileName,
                dataset.CreatedAt,
                dataset.UserId);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var dataset = await _context.Datasets.FindAsync([id], ct);

            if (dataset is null) return false;

            _context.Datasets.Remove(dataset);
            await _context.SaveChangesAsync(ct);

            return true;
        }
    }
}