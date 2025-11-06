using ChatApp.Data;
using ChatApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace ChatApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly AppDbContext _db;

        public MessageController(AppDbContext db)
        {
            _db = db;
        }

        // 🔹 Helper method để lấy UserId từ claims
        private int? GetUserIdFromClaims()
        {
            try
            {
                // Try multiple claim types to find user ID
                // Priority: 1. Custom "userId" claim, 2. JwtRegisteredClaimNames.NameId, 3. ClaimTypes.NameIdentifier
                var userIdClaim = User.FindFirst("userId")?.Value 
                    ?? User.FindFirst(JwtRegisteredClaimNames.NameId)?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                Console.WriteLine($"[MessageController] Attempting to get userId from claims:");
                Console.WriteLine($"[MessageController] - 'userId' claim: '{User.FindFirst("userId")?.Value ?? "null"}'");
                Console.WriteLine($"[MessageController] - 'NameId' claim: '{User.FindFirst(JwtRegisteredClaimNames.NameId)?.Value ?? "null"}'");
                Console.WriteLine($"[MessageController] - 'ClaimTypes.NameIdentifier' claim: '{User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "null"}'");
                Console.WriteLine($"[MessageController] Selected userIdClaim value: '{userIdClaim}'");
                
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    Console.WriteLine("[MessageController] userIdClaim is null or empty");
                    return null;
                }
                
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    Console.WriteLine($"[MessageController] Failed to parse userIdClaim '{userIdClaim}' as integer");
                    return null;
                }
                
                if (userId == 0)
                {
                    Console.WriteLine("[MessageController] userId is 0");
                    return null;
                }
                
                Console.WriteLine($"[MessageController] Successfully parsed userId: {userId}");
                return userId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MessageController] Exception in GetUserIdFromClaims: {ex.Message}");
                return null;
            }
        }

        // 🔹 Lấy tin nhắn theo Room
        [HttpGet("room/{roomId}")]
        [Authorize]
        public async Task<IActionResult> GetByRoom(int roomId)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (!userId.HasValue)
                {
                    return Unauthorized("Invalid user token");
                }
                
                // Check if user is member of the room
                var isMember = await _db.RoomMembers
                    .AnyAsync(rm => rm.RoomId == roomId && rm.UserId == userId.Value);
                
                if (!isMember)
                    return Forbid("You are not a member of this room");

                var messages = await _db.Messages
                    .Where(m => m.RoomId == roomId)
                    .OrderBy(m => m.Timestamp)
                    .Include(m => m.SenderUser)
                    .Select(m => new
                    {
                        m.Id,
                        m.SenderId,
                        Sender = m.SenderUser!.Username,
                        m.Content,
                        m.Timestamp,
                        m.RoomId
                    })
                    .ToListAsync();
                    
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading messages", error = ex.Message });
            }
        }

        // 🔹 Lấy tin nhắn DM giữa 2 users
        [HttpGet("dm/{targetUserId}")]
        [Authorize]
        public async Task<IActionResult> GetDM(int targetUserId)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (!userId.HasValue)
                {
                    return Unauthorized("Invalid user token");
                }

                // Find DM room between these two users
                var dmRoomId = await _db.RoomMembers
                    .Where(rm => rm.UserId == userId.Value && rm.Room != null && rm.Room.IsPrivate)
                    .Where(rm => rm.Room!.Members.Any(m => m.UserId == targetUserId))
                    .Include(rm => rm.Room)
                    .Select(rm => rm.RoomId)
                    .FirstOrDefaultAsync();

                if (dmRoomId == 0)
                    return Ok(new List<object>()); // No DM room exists yet

                var messages = await _db.Messages
                    .Where(m => m.RoomId == dmRoomId && (m.SenderId == userId.Value || m.SenderId == targetUserId))
                    .OrderBy(m => m.Timestamp)
                    .Include(m => m.SenderUser)
                    .Select(m => new
                    {
                        m.Id,
                        m.SenderId,
                        Sender = m.SenderUser!.Username,
                        m.Content,
                        m.Timestamp,
                        m.RoomId
                    })
                    .ToListAsync();

                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading DM messages", error = ex.Message });
            }
        }

        // 🔹 Lấy tin nhắn theo ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var message = await _db.Messages.FindAsync(id);
            if (message == null) return NotFound();
            return Ok(message);
        }

        // 🔹 Gửi tin nhắn mới
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] MessageCreateRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Content))
                {
                    return BadRequest("Message content is required");
                }

                var userId = GetUserIdFromClaims();
                if (!userId.HasValue)
                {
                    return Unauthorized("Invalid user token");
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "";

                var user = await _db.Users.FindAsync(userId.Value);
                if (user == null)
                    return Unauthorized("User not found");

                int? finalRoomId = request.RoomId;

                // If ReceiverId is provided (DM), find or create DM room
                if (request.ReceiverId.HasValue && !finalRoomId.HasValue)
                {
                    var targetUserId = request.ReceiverId.Value;

                    // Find existing DM room
                    var existingDM = await _db.RoomMembers
                        .Where(rm => rm.UserId == userId.Value && rm.Room != null && rm.Room.IsPrivate)
                        .Where(rm => rm.Room!.Members.Any(m => m.UserId == targetUserId))
                        .Include(rm => rm.Room)
                        .FirstOrDefaultAsync();

                    if (existingDM != null)
                    {
                        finalRoomId = existingDM.RoomId;
                    }
                    else
                    {
                        // Create new DM room
                        var targetUser = await _db.Users.FindAsync(targetUserId);
                        if (targetUser == null)
                            return BadRequest("Target user not found");

                        var dmRoom = new Room
                        {
                            Name = $"DM-{userId.Value}-{targetUserId}",
                            Description = "",
                            IsPrivate = true
                        };

                        _db.Rooms.Add(dmRoom);
                        await _db.SaveChangesAsync();

                        // Add both users as members
                        _db.RoomMembers.Add(new RoomMember { RoomId = dmRoom.Id, UserId = userId.Value });
                        _db.RoomMembers.Add(new RoomMember { RoomId = dmRoom.Id, UserId = targetUserId });
                        await _db.SaveChangesAsync();

                        finalRoomId = dmRoom.Id;
                    }
                }

                // Validate room access if RoomId provided
                if (finalRoomId.HasValue)
                {
                    var isMember = await _db.RoomMembers
                        .AnyAsync(rm => rm.RoomId == finalRoomId && rm.UserId == userId.Value);
                    
                    if (!isMember)
                        return Forbid("You are not a member of this room");
                }

                var message = new Message
                {
                    SenderId = userId.Value,
                    Sender = username,
                    RoomId = finalRoomId,
                    ReceiverId = request.ReceiverId,
                    Content = request.Content,
                    Timestamp = DateTime.UtcNow
                };

                _db.Messages.Add(message);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message.Id,
                    message.SenderId,
                    message.Sender,
                    message.Content,
                    message.Timestamp,
                    message.RoomId,
                    message.ReceiverId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error sending message", error = ex.Message });
            }
        }

        // 🔹 Xóa tin nhắn
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var message = await _db.Messages.FindAsync(id);
            if (message == null) return NotFound();

            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (message.Sender != username)
                return Forbid("You can only delete your own messages.");

            _db.Messages.Remove(message);
            await _db.SaveChangesAsync();

            return Ok();
        }
    }

    public class MessageCreateRequest
    {
        public string Content { get; set; } = string.Empty;
        public int? RoomId { get; set; } // For room messages
        public int? ReceiverId { get; set; } // For DM messages
    }
}

