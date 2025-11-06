using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;
using ChatApp.Data;
using ChatApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ChatApp.Services
{
    public class AuthService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // 🔹 Băm mật khẩu
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // 🔹 Kiểm tra mật khẩu
        public bool VerifyPassword(string plainPassword, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
        }

        // 🔹 Tạo JWT Token
        public string CreateAccessToken(User user)
        {
            var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var userIdString = user.Id.ToString();
            Console.WriteLine($"[AuthService] Creating token for user ID: {user.Id}, Username: {user.Username}");
            Console.WriteLine($"[AuthService] Setting userId claim to: '{userIdString}'");

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim("userId", userIdString), // Use custom claim name to avoid mapping conflicts
                new Claim(ClaimTypes.NameIdentifier, userIdString), // Keep for compatibility
                new Claim(JwtRegisteredClaimNames.NameId, userIdString), // Alternative standard claim
                new Claim(ClaimTypes.Name, user.Username),
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            Console.WriteLine($"[AuthService] Token created successfully");
            return tokenString;
        }

        // 🔹 Xác thực người dùng (đăng nhập)
        public async Task<User?> AuthenticateUserAsync(string username, string password)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || !VerifyPassword(password, user.PasswordHash))
                return null;
            return user;
        }

        // 🔹 Giải mã token và lấy user
        public async Task<User?> GetUserFromTokenAsync(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var username = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
                if (username == null) return null;

                return await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            }
            catch
            {
                return null;
            }
        }
    }
}
