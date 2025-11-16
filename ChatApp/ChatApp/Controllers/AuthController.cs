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
            try
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
            catch (Microsoft.Data.SqlClient.SqlException sqlEx)
            {
                // Lỗi SQL Server connection
                var errorMessage = "Database connection error. ";
                if (sqlEx.Message.Contains("network-related") || sqlEx.Message.Contains("actively refused"))
                {
                    errorMessage += "Cannot connect to SQL Server. Please check:\n" +
                                   "1. SQL Server service is running\n" +
                                   "2. Connection string is correct in appsettings.json\n" +
                                   "3. Database exists (run SetupDatabase.sql)\n" +
                                   "4. Firewall allows port 1433\n\n" +
                                   "Tip: Use SQLite for easier setup (see SETUP_GUIDE.md)";
                }
                else
                {
                    errorMessage += sqlEx.Message;
                }
                return StatusCode(500, new { message = errorMessage, error = sqlEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Registration failed. " + ex.Message });
            }
        }

        // 🔹 Đăng nhập để lấy token
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User creds)
        {
            try
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
            catch (Microsoft.Data.SqlClient.SqlException sqlEx)
            {
                // Lỗi SQL Server connection
                var errorMessage = "Database connection error. ";
                if (sqlEx.Message.Contains("network-related") || sqlEx.Message.Contains("actively refused"))
                {
                    errorMessage += "Cannot connect to SQL Server. Please check SQL Server configuration (see SETUP_GUIDE.md)";
                }
                else
                {
                    errorMessage += sqlEx.Message;
                }
                return StatusCode(500, new { message = errorMessage });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Login failed. " + ex.Message });
            }
        }
    }
}
