using Microsoft.EntityFrameworkCore;
using RetailForecast.Data;
using RetailForecast.DTOs.Dataset;
using RetailForecast.DTOs.User;
using RetailForecast.Entities;
using System.Security.Cryptography;
using System.Text;

namespace RetailForecast.Services
{
    public class UserService
    {
        private readonly RetailForecastDbContext _context;
        private readonly JwtService _jwtService;

        public UserService(RetailForecastDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var salt = new byte[16];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(salt);
                }

                var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
                var hash = pbkdf2.GetBytes(20);

                var hashWithSalt = new byte[36];
                Array.Copy(salt, 0, hashWithSalt, 0, 16);
                Array.Copy(hash, 0, hashWithSalt, 16, 20);

                return Convert.ToBase64String(hashWithSalt);
            }
        }

        private static bool VerifyPassword(string password, string hash)
        {
            var hashWithSalt = Convert.FromBase64String(hash);
            var salt = new byte[16];
            Array.Copy(hashWithSalt, 0, salt, 0, 16);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            var hash2 = pbkdf2.GetBytes(20);

            for (int i = 0; i < 20; i++)
                if (hashWithSalt[i + 16] != hash2[i])
                    return false;

            return true;
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
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Email and password are required");

            if (!IsValidEmail(request.Email))
                throw new ArgumentException("Invalid email format");

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, ct);
            
            if (existingUser is not null)
                throw new InvalidOperationException("User with this email already exists");

            var user = new User
            {
                Email = request.Email,
                PasswordHash = HashPassword(request.Password)
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
            if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Password))
                return null;

            var user = await _context.Users.FindAsync([id], ct);

            if (user is null) return null;

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                if (!IsValidEmail(request.Email))
                    throw new ArgumentException("Invalid email format");

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != id, ct);
                
                if (existingUser is not null)
                    throw new InvalidOperationException("User with this email already exists");

                user.Email = request.Email;
            }

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.PasswordHash = HashPassword(request.Password);
            }

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
            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Email and password are required");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, ct);
            
            if (user is null)
                throw new UnauthorizedAccessException("Invalid email or password");

            if (!VerifyPassword(request.Password, user.PasswordHash ?? ""))
                throw new UnauthorizedAccessException("Invalid email or password");

            var token = _jwtService.GenerateToken(user);

            return new AuthResponse(user.Id, user.Email, token);
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
