using Microsoft.EntityFrameworkCore;
using RetailForecast.Data;
using RetailForecast.DTOs.Model;
using RetailForecast.Entities;

namespace RetailForecast.Services
{
    public class ModelService
    {
        private readonly RetailForecastDbContext _context;

        public ModelService(RetailForecastDbContext context)
        {
            _context = context;
        }

        public async Task<List<ModelResponse>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Models
                .AsNoTracking()
                .Select(m => new ModelResponse(
                    m.Id,
                    m.Name,
                    m.Algorithm,
                    m.Description,
                    m.CreatedAt,
                    m.UpdatedAt
                    ))
                .ToListAsync(ct);
        }

        public async Task<ModelResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var model = await _context.Models
                .FindAsync([id], ct);

            if (model is null) return null;

            return new ModelResponse(
                model.Id,
                model.Name,
                model.Algorithm,
                model.Description,
                model.CreatedAt,
                model.UpdatedAt);
        }

        public async Task<ModelResponse> CreateAsync(
            CreateModelRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Algorithm))
                throw new ArgumentException("Name and Algorithm are required");

            var model = new Model
            {
                Name = request.Name,
                Algorithm = request.Algorithm,
                Description = request.Description
            };

            _context.Models.Add(model);
            await _context.SaveChangesAsync(ct);

            return new ModelResponse(
                model.Id,
                model.Name,
                model.Algorithm,
                model.Description,
                model.CreatedAt,
                model.UpdatedAt);
        }

        public async Task<ModelResponse?> UpdateAsync(
            int id, UpdateModelRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name) && string.IsNullOrWhiteSpace(request.Algorithm) && string.IsNullOrWhiteSpace(request.Description))
                return null;

            var model = await _context.Models.FindAsync([id], ct);

            if (model is null) return null;

            if (!string.IsNullOrWhiteSpace(request.Name))
                model.Name = request.Name;

            if (!string.IsNullOrWhiteSpace(request.Algorithm))
                model.Algorithm = request.Algorithm;

            if (!string.IsNullOrWhiteSpace(request.Description))
                model.Description = request.Description;

            await _context.SaveChangesAsync(ct);

            return new ModelResponse(
                model.Id,
                model.Name,
                model.Algorithm,
                model.Description,
                model.CreatedAt,
                model.UpdatedAt);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var model = await _context.Models.FindAsync([id], ct);

            if (model is null) return false;

            _context.Models.Remove(model);
            await _context.SaveChangesAsync(ct);

            return true;
        }
    }
}
