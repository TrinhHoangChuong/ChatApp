using ChatApp.Data;
using ChatApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace ChatApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoomController : ControllerBase
    {
        private readonly AppDbContext _db;

        public RoomController(AppDbContext db)
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
                
                Console.WriteLine($"[RoomController] Attempting to get userId from claims:");
                Console.WriteLine($"[RoomController] - 'userId' claim: '{User.FindFirst("userId")?.Value ?? "null"}'");
                Console.WriteLine($"[RoomController] - 'NameId' claim: '{User.FindFirst(JwtRegisteredClaimNames.NameId)?.Value ?? "null"}'");
                Console.WriteLine($"[RoomController] - 'ClaimTypes.NameIdentifier' claim: '{User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "null"}'");
                Console.WriteLine($"[RoomController] Selected userIdClaim value: '{userIdClaim}'");
                
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    Console.WriteLine("[RoomController] userIdClaim is null or empty");
                    return null;
                }
                
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    Console.WriteLine($"[RoomController] Failed to parse userIdClaim '{userIdClaim}' as integer");
                    return null;
                }
                
                if (userId == 0)
                {
                    Console.WriteLine("[RoomController] userId is 0");
                    return null;
                }
                
                Console.WriteLine($"[RoomController] Successfully parsed userId: {userId}");
                return userId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RoomController] Exception in GetUserIdFromClaims: {ex.Message}");
                return null;
            }
        }

        // 🔹 Lấy tất cả rooms (public rooms và DMs của user)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (!userId.HasValue)
                {
                    return Unauthorized("Invalid user token");
                }
                
                // Get public rooms
                var publicRooms = await _db.Rooms
                    .Where(r => !r.IsPrivate)
                    .Include(r => r.Members)
                    .Select(r => new
                    {
                        r.Id,
                        r.Name,
                        r.Description,
                        r.IsPrivate,
                        MemberCount = r.Members.Count
                    })
                    .ToListAsync();

                // Get DMs (private rooms) where user is a member
                var userDMs = await _db.RoomMembers
                    .Where(rm => rm.UserId == userId.Value && rm.Room != null && rm.Room.IsPrivate)
                    .Include(rm => rm.Room!)
                    .ThenInclude(r => r!.Members)
                    .ThenInclude(m => m.User)
                    .Select(rm => new
                    {
                        rm.Room!.Id,
                        Name = rm.Room.Members
                            .Where(m => m.UserId != userId.Value)
                            .Select(m => m.User != null ? m.User.Username : "Unknown")
                            .FirstOrDefault() ?? "Unknown",
                        Description = "",
                        IsPrivate = true,
                        MemberCount = rm.Room.Members.Count
                    })
                    .ToListAsync();

                return Ok(new { PublicRooms = publicRooms, DirectMessages = userDMs });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading rooms", error = ex.Message });
            }
        }

        // 🔹 Tạo room mới (public)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRoomRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest("Room name is required");
                }

                var userId = GetUserIdFromClaims();
                if (!userId.HasValue)
                {
                    return Unauthorized("Invalid user token");
                }

                var room = new Room
                {
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim() ?? "",
                    IsPrivate = false,
                    CreatorId = userId.Value
                };

                _db.Rooms.Add(room);
                await _db.SaveChangesAsync();

                // Add creator as member
                var member = new RoomMember
                {
                    RoomId = room.Id,
                    UserId = userId.Value
                };
                _db.RoomMembers.Add(member);
                await _db.SaveChangesAsync();

                return Ok(new { Id = room.Id, Name = room.Name, Description = room.Description });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating room", error = ex.Message });
            }
        }

        // 🔹 Tham gia room
        [HttpPost("{roomId}/join")]
        public async Task<IActionResult> JoinRoom(int roomId)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (!userId.HasValue)
                {
                    return Unauthorized("Invalid user token");
                }

                var room = await _db.Rooms.FindAsync(roomId);
                if (room == null) return NotFound("Room not found");

                // Check if already a member
                var existingMember = await _db.RoomMembers
                    .FirstOrDefaultAsync(rm => rm.RoomId == roomId && rm.UserId == userId.Value);
                
                if (existingMember != null)
                    return BadRequest("Already a member of this room");

                var member = new RoomMember
                {
                    RoomId = roomId,
                    UserId = userId.Value
                };

                _db.RoomMembers.Add(member);
                await _db.SaveChangesAsync();

                return Ok(new { message = "Joined room successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error joining room", error = ex.Message });
            }
        }

        // 🔹 Rời room
        [HttpPost("{roomId}/leave")]
        public async Task<IActionResult> LeaveRoom(int roomId)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (!userId.HasValue)
                {
                    return Unauthorized("Invalid user token");
                }

                var member = await _db.RoomMembers
                    .FirstOrDefaultAsync(rm => rm.RoomId == roomId && rm.UserId == userId.Value);
                
                if (member == null)
                    return NotFound("You are not a member of this room");

                _db.RoomMembers.Remove(member);
                await _db.SaveChangesAsync();

                return Ok(new { message = "Left room successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error leaving room", error = ex.Message });
            }
        }

        // 🔹 Tạo DM với user khác
        [HttpPost("dm/{targetUserId}")]
        public async Task<IActionResult> CreateDM(int targetUserId)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (!userId.HasValue)
                {
                    return Unauthorized("Invalid user token");
                }

                if (userId.Value == targetUserId)
                    return BadRequest("Cannot create DM with yourself");

                var targetUser = await _db.Users.FindAsync(targetUserId);
                if (targetUser == null)
                    return NotFound("Target user not found");

                // Check if DM already exists
                var existingDM = await _db.RoomMembers
                    .Where(rm => rm.UserId == userId.Value && rm.Room != null && rm.Room.IsPrivate)
                    .Where(rm => rm.Room!.Members.Any(m => m.UserId == targetUserId))
                    .Include(rm => rm.Room)
                    .FirstOrDefaultAsync();

                if (existingDM != null)
                    return Ok(new { RoomId = existingDM.RoomId, message = "DM already exists" });

                // Create new DM room
                var dmRoom = new Room
                {
                    Name = $"DM-{userId.Value}-{targetUserId}", // Internal name
                    Description = "",
                    IsPrivate = true
                };

                _db.Rooms.Add(dmRoom);
                await _db.SaveChangesAsync();

                // Add both users as members
                _db.RoomMembers.Add(new RoomMember { RoomId = dmRoom.Id, UserId = userId.Value });
                _db.RoomMembers.Add(new RoomMember { RoomId = dmRoom.Id, UserId = targetUserId });
                await _db.SaveChangesAsync();

                return Ok(new { RoomId = dmRoom.Id, TargetUser = targetUser.Username });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating DM", error = ex.Message });
            }
        }

        // 🔹 Lấy members của room
        [HttpGet("{roomId}/members")]
        public async Task<IActionResult> GetMembers(int roomId)
        {
            var members = await _db.RoomMembers
                .Where(rm => rm.RoomId == roomId)
                .Include(rm => rm.User)
                .Select(rm => new
                {
                    rm.UserId,
                    rm.User!.Username,
                    rm.JoinedAt
                })
                .ToListAsync();

            return Ok(members);
        }
    }

    public class CreateRoomRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}

