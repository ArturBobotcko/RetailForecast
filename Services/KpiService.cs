using Microsoft.EntityFrameworkCore;
using RetailForecast.Data;
using RetailForecast.DTOs.Kpi;
using RetailForecast.Entities;

namespace RetailForecast.Services
{
    public class KpiService
    {
        private readonly RetailForecastDbContext _context;

        public KpiService(RetailForecastDbContext context)
        {
            _context = context;
        }

        public async Task<List<KpiResponse>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Kpis
                .AsNoTracking()
                .Select(k => new KpiResponse(
                    k.Id,
                    k.Name,
                    k.DataType,
                    k.DatasetId,
                    k.CreatedAt,
                    k.UpdatedAt))
                .ToListAsync(ct);
        }

        public async Task<KpiResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var kpi = await _context.Kpis.FindAsync([id], ct);

            if (kpi is null) return null;

            return new KpiResponse(
                kpi.Id,
                kpi.Name,
                kpi.DataType,
                kpi.DatasetId,
                kpi.CreatedAt,
                kpi.UpdatedAt);
        }

        public async Task<KpiResponse?> CreateAsync(
            CreateKpiRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.DataType))
                throw new ArgumentException("Name and DataType are required");

            var dataset = await _context.Datasets.FindAsync([request.DatasetId], ct);
            if (dataset is null)
                throw new InvalidOperationException("Dataset not found");

            var kpi = new Kpi
            {
                Name = request.Name,
                DataType = request.DataType,
                DatasetId = request.DatasetId
            };

            _context.Kpis.Add(kpi);
            await _context.SaveChangesAsync(ct);

            return new KpiResponse(
                kpi.Id,
                kpi.Name,
                kpi.DataType,
                kpi.DatasetId,
                kpi.CreatedAt,
                kpi.UpdatedAt);
        }

        public async Task<KpiResponse?> UpdateAsync(
            int id, UpdateKpiRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name) && string.IsNullOrWhiteSpace(request.DataType))
                return null;

            var kpi = await _context.Kpis.FindAsync([id], ct);

            if (kpi is null) return null;

            if (!string.IsNullOrWhiteSpace(request.Name))
                kpi.Name = request.Name;

            if (!string.IsNullOrWhiteSpace(request.DataType))
                kpi.DataType = request.DataType;

            await _context.SaveChangesAsync(ct);

            return new KpiResponse(
                kpi.Id,
                kpi.Name,
                kpi.DataType,
                kpi.DatasetId,
                kpi.CreatedAt,
                kpi.UpdatedAt);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var kpi = await _context.Kpis.FindAsync([id], ct);

            if (kpi is null) return false;

            _context.Kpis.Remove(kpi);
            await _context.SaveChangesAsync(ct);

            return true;
        }
    }
}
