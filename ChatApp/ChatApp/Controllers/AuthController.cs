using ChatApp.Data;
using ChatApp.Models;
using ChatApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace ChatApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly AppDbContext _db;

        public AuthController(AuthService authService, AppDbContext db)
        {
            _authService = authService;
            _db = db;
        }

        // 🔹 Đăng ký tài khoản mới
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (string.IsNullOrWhiteSpace(user.Username))
                return BadRequest("Username is required.");
            
            if (string.IsNullOrWhiteSpace(user.PasswordHash))
                return BadRequest("Password is required.");
            
            // Kiểm tra username không phân biệt hoa thường
            if (await _db.Users.AnyAsync(u => u.Username.ToLower() == user.Username.ToLower()))
                return BadRequest("Username already exists.");

            // Hash password trước khi lưu
            var plainPassword = user.PasswordHash; // Lưu plain password tạm
            user.PasswordHash = _authService.HashPassword(plainPassword);
            
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return Ok(new { message = "User created successfully" });
        }

        // 🔹 Đăng nhập để lấy token
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User creds)
        {
            if (string.IsNullOrWhiteSpace(creds.Username))
                return BadRequest("Username is required.");
            
            if (string.IsNullOrWhiteSpace(creds.PasswordHash))
                return BadRequest("Password is required.");
            
            var user = await _authService.AuthenticateUserAsync(creds.Username, creds.PasswordHash);
            if (user == null)
                return Unauthorized("Invalid username or password.");

            var token = _authService.CreateAccessToken(user);
            return Ok(new { token });
        }
    }
}
