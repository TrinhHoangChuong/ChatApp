using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatApp.Data;
using ChatApp.Models;
using ChatApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FriendsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly AuthService _authService;

        public FriendsController(AppDbContext db, AuthService authService)
        {
            _db = db;
            _authService = authService;
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var header))
                return null;

            var token = header.ToString();
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring("Bearer ".Length);
            }

            if (string.IsNullOrWhiteSpace(token))
                return null;

            return await _authService.GetUserFromTokenAsync(token);
        }

        [HttpGet]
        public async Task<IActionResult> GetFriends()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var friendIds = await _db.Friendships
                .Where(f => f.UserId == user.Id)
                .Select(f => f.FriendId)
                .ToListAsync();

            if (friendIds.Count == 0)
            {
                return Ok(Array.Empty<FriendDto>());
            }

            var friends = await _db.Users
                .Where(u => friendIds.Contains(u.Id))
                .Select(u => new FriendDto(u.Id, u.Username))
                .ToListAsync();

            return Ok(friends);
        }

        [HttpGet("requests")]
        public async Task<IActionResult> GetRequests()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var received = await _db.FriendRequests
                .Where(r => r.ReceiverId == user.Id && r.Status == "Pending")
                .Join(_db.Users, r => r.SenderId, u => u.Id, (r, u) => new FriendRequestDto(r.Id, u.Id, u.Username, r.Status, r.CreatedAt))
                .ToListAsync();

            var sent = await _db.FriendRequests
                .Where(r => r.SenderId == user.Id && r.Status == "Pending")
                .Join(_db.Users, r => r.ReceiverId, u => u.Id, (r, u) => new FriendRequestDto(r.Id, u.Id, u.Username, r.Status, r.CreatedAt))
                .ToListAsync();

            return Ok(new FriendRequestListDto(received, sent));
        }

        [HttpPost("request")]
        public async Task<IActionResult> SendRequest([FromBody] FriendRequestCreate request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest("Username không được để trống.");
            }

            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            if (string.Equals(user.Username, request.Username, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Bạn không thể kết bạn với chính mình.");
            }

            var target = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (target == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            var existingFriend = await _db.Friendships
                .AnyAsync(f => f.UserId == user.Id && f.FriendId == target.Id);
            if (existingFriend)
            {
                return Conflict("Bạn đã là bạn bè.");
            }

            var pending = await _db.FriendRequests.FirstOrDefaultAsync(r =>
                ((r.SenderId == user.Id && r.ReceiverId == target.Id) ||
                 (r.SenderId == target.Id && r.ReceiverId == user.Id)) &&
                 r.Status == "Pending");
            if (pending != null)
            {
                return Conflict("Đã tồn tại lời mời kết bạn đang chờ.");
            }

            var friendRequest = new FriendRequest
            {
                SenderId = user.Id,
                ReceiverId = target.Id,
                Status = "Pending"
            };
            _db.FriendRequests.Add(friendRequest);
            await _db.SaveChangesAsync();

            return Ok(new FriendRequestDto(friendRequest.Id, target.Id, target.Username, friendRequest.Status, friendRequest.CreatedAt));
        }

        [HttpPost("requests/{requestId}/accept")]
        public async Task<IActionResult> AcceptRequest(int requestId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var request = await _db.FriendRequests.FirstOrDefaultAsync(r => r.Id == requestId && r.ReceiverId == user.Id);
            if (request == null) return NotFound();
            if (request.Status != "Pending") return BadRequest("Yêu cầu đã được xử lý.");

            request.Status = "Accepted";
            request.RespondedAt = DateTime.UtcNow;

            if (!await _db.Friendships.AnyAsync(f => f.UserId == user.Id && f.FriendId == request.SenderId))
            {
                _db.Friendships.Add(new Friendship { UserId = user.Id, FriendId = request.SenderId });
            }
            if (!await _db.Friendships.AnyAsync(f => f.UserId == request.SenderId && f.FriendId == user.Id))
            {
                _db.Friendships.Add(new Friendship { UserId = request.SenderId, FriendId = user.Id });
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("requests/{requestId}/reject")]
        public async Task<IActionResult> RejectRequest(int requestId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var request = await _db.FriendRequests.FirstOrDefaultAsync(r => r.Id == requestId && r.ReceiverId == user.Id);
            if (request == null) return NotFound();
            if (request.Status != "Pending") return BadRequest("Yêu cầu đã được xử lý.");

            request.Status = "Rejected";
            request.RespondedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{friendId}")]
        public async Task<IActionResult> RemoveFriend(int friendId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var entries = await _db.Friendships
                .Where(f => (f.UserId == user.Id && f.FriendId == friendId) || (f.UserId == friendId && f.FriendId == user.Id))
                .ToListAsync();

            if (entries.Count == 0)
            {
                return NotFound();
            }

            _db.Friendships.RemoveRange(entries);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        public record FriendRequestCreate
        {
            public string Username { get; set; } = string.Empty;
        }

        private record FriendDto(int Id, string Username);

        private record FriendRequestDto(int RequestId, int UserId, string Username, string Status, DateTime CreatedAt);

        private record FriendRequestListDto(IEnumerable<FriendRequestDto> Incoming, IEnumerable<FriendRequestDto> Outgoing);
    }
}

