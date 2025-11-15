using Microsoft.AspNetCore.Mvc;
using ChatApp.Data;
using ChatApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace ChatApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserRepository _userRepo;

        public UserController(UserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var users = _userRepo.GetAllUsers();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var user = _userRepo.GetUserById(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpGet("username/{username}")]
        public IActionResult GetByUsername(string username)
        {
            var user = _userRepo.GetUserByUsername(username);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public IActionResult Create([FromBody] UserCreateRequest request)
        {
            // (Optional) kiểm tra tồn tại username trước khi tạo
            var existing = _userRepo.GetUserByUsername(request.Username);
            if (existing != null) return BadRequest("Username đã tồn tại");

            var user = _userRepo.CreateUser(request.Username, request.Password);
            return Ok(new { user.Id, user.Username });
        }

        [HttpPut("{id}")]
        [Authorize] // bắt buộc token, tuỳ bạn
        public IActionResult Update(int id, [FromBody] UpdateUsernameRequest request)
        {
            var success = _userRepo.UpdateUser(id, request.NewUsername);
            return success ? Ok() : NotFound();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public IActionResult Delete(int id)
        {
            var success = _userRepo.DeleteUser(id);
            return success ? Ok() : NotFound();
        }
    }

    public class UserCreateRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UpdateUsernameRequest
    {
        public string NewUsername { get; set; } = string.Empty;
    }
}
