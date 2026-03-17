using Microsoft.EntityFrameworkCore;
using RetailForecast.Data;
using RetailForecast.DTOs.Dataset;
using RetailForecast.DTOs.User;
using RetailForecast.Entities;

namespace RetailForecast.Services
{
    public class UserService
    {
        private readonly RetailForecastDbContext _context;

        public UserService(RetailForecastDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserResponse>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Users
                .AsNoTracking()
                .Select(u => new UserResponse(
                    u.Id,
                    u.Email,
                    u.CreatedAt))
                .ToListAsync(ct);
        }

        public async Task<UserResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var user = await _context.Users.FindAsync([id], ct);

            if (user is null) return null;

            return new UserResponse(
                user.Id,
                user.Email,
                user.CreatedAt);
        }

        public async Task<UserResponse> CreateAsync(
            CreateUserRequest request, CancellationToken ct = default)
        {
            var user = new User
            {
                Email = request.Email,
                PasswordHash = request.PasswordHash
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(ct);

            return new UserResponse(
                user.Id,
                user.Email,
                user.CreatedAt);
        }


        public async Task<UserResponse?> UpdateAsync(
            int id, UpdateUserRequest request, CancellationToken ct = default)
        {
            var user = await _context.Users.FindAsync([id], ct);

            if (user is null) return null;

            user.Email = request.Email;
            user.PasswordHash = request.PasswordHash;

            await _context.SaveChangesAsync(ct);

            return new UserResponse(
                user.Id,
                user.Email,
                user.CreatedAt);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var user = await _context.Users.FindAsync([id], ct);

            if (user is null) return false;

            _context.Users.Remove(user);

            return true;
        }
    }
}
