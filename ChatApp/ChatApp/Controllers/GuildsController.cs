using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatApp.Data;
using ChatApp.Models;
using ChatApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using ChatApp.Hubs;

namespace ChatApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GuildsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly AuthService _authService;
        private readonly IHubContext<ChatHub> _hubContext;

        public GuildsController(AppDbContext db, AuthService authService, IHubContext<ChatHub> hubContext)
        {
            _db = db;
            _authService = authService;
            _hubContext = hubContext;
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var header))
                return null;

            var token = header.ToString();
            if (token.StartsWith("Bearer "))
            {
                token = token.Substring("Bearer ".Length);
            }

            if (string.IsNullOrWhiteSpace(token))
                return null;

            return await _authService.GetUserFromTokenAsync(token);
        }

        [HttpGet]
        public async Task<IActionResult> GetGuilds()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            // Lấy tất cả guilds mà user là member (owner hoặc member)
            var memberships = await _db.GuildMemberships
                .Where(m => m.UserId == user.Id)
                .ToListAsync();

            if (memberships.Count == 0)
            {
                // Nếu chưa có guild nào, kiểm tra xem có guild mà user là owner không
                var ownedGuild = await _db.Guilds
                    .FirstOrDefaultAsync(g => g.OwnerId == user.Id);
                
                if (ownedGuild != null)
                {
                    // Tạo membership cho owner
                    var membership = new GuildMembership
                    {
                        GuildId = ownedGuild.Id,
                        UserId = user.Id,
                        Role = "owner"
                    };
                    _db.GuildMemberships.Add(membership);
                    await _db.SaveChangesAsync();
                    memberships.Add(membership);
                }
                else
                {
                    return Ok(new object[0]);
                }
            }

            var guildIds = memberships.Select(m => m.GuildId).ToList();
            var guilds = await _db.Guilds.Where(g => guildIds.Contains(g.Id)).ToListAsync();

            // Lấy channels mà user có quyền truy cập
            var userChannelIds = await _db.ChannelMemberships
                .Where(cm => cm.UserId == user.Id)
                .Select(cm => cm.ChannelId)
                .ToListAsync();

            var result = guilds.Select(guild =>
            {
                var membership = memberships.First(m => m.GuildId == guild.Id);
                
                var channels = _db.Channels
                    .Where(c => c.GuildId == guild.Id && 
                               (userChannelIds.Contains(c.Id) || guild.OwnerId == user.Id))
                    .OrderBy(c => c.Name)
                    .Select(c => new ChannelDto(c.Id, c.Name, c.Topic))
                    .ToList();

                return new GuildDto(
                    guild.Id,
                    guild.Name,
                    guild.Description ?? string.Empty,
                    guild.OwnerId,
                    membership.Role,
                    channels);
            }).ToList();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateGuild([FromBody] CreateGuildRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Tên máy chủ không được để trống.");
            }

            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            // Kiểm tra xem user đã có server chưa
            var existingGuild = await _db.Guilds
                .FirstOrDefaultAsync(g => g.OwnerId == user.Id);
            if (existingGuild != null)
            {
                return Conflict(new { message = "Bạn đã có máy chủ riêng. Mỗi người dùng chỉ có thể có 1 máy chủ.", guildId = existingGuild.Id });
            }

            var guild = new Guild
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                OwnerId = user.Id
            };
            _db.Guilds.Add(guild);
            await _db.SaveChangesAsync();

            var membership = new GuildMembership
            {
                GuildId = guild.Id,
                UserId = user.Id,
                Role = "owner"
            };
            _db.GuildMemberships.Add(membership);

            var defaultChannel = new Channel
            {
                GuildId = guild.Id,
                Name = "general",
                Topic = "Kênh mặc định của máy chủ"
            };
            _db.Channels.Add(defaultChannel);
            await _db.SaveChangesAsync();

            // Thêm owner vào channel "general" membership
            var defaultChannelMembership = new ChannelMembership
            {
                ChannelId = defaultChannel.Id,
                UserId = user.Id
            };
            _db.ChannelMemberships.Add(defaultChannelMembership);
            await _db.SaveChangesAsync();

            var dto = new GuildDto(
                guild.Id,
                guild.Name,
                guild.Description ?? string.Empty,
                guild.OwnerId,
                membership.Role,
                new[] { new ChannelDto(defaultChannel.Id, defaultChannel.Name, defaultChannel.Topic) });

            return Ok(dto);
        }

        [HttpPost("{guildId}/channels")]
        public async Task<IActionResult> CreateChannel(int guildId, [FromBody] CreateChannelRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Tên kênh không được để trống.");
            }

            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var membership = await _db.GuildMemberships
                .FirstOrDefaultAsync(m => m.GuildId == guildId && m.UserId == user.Id);
            if (membership == null)
            {
                return Forbid();
            }

            var channel = new Channel
            {
                GuildId = guildId,
                Name = request.Name.Trim(),
                Topic = request.Topic?.Trim()
            };

            _db.Channels.Add(channel);
            await _db.SaveChangesAsync();

            // Chỉ thêm người tạo vào channel membership (không tự động thêm tất cả members)
            var channelMembership = new ChannelMembership
            {
                ChannelId = channel.Id,
                UserId = user.Id
            };
            _db.ChannelMemberships.Add(channelMembership);
            await _db.SaveChangesAsync();

            var dto = new ChannelDto(channel.Id, channel.Name, channel.Topic);

            await _hubContext.Clients.Group($"guild:{guildId}")
                .SendAsync("GuildChannelCreated", guildId, dto);

            return Ok(dto);
        }

        [HttpPost("{guildId}/join")]
        public async Task<IActionResult> JoinGuild(int guildId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var guild = await _db.Guilds.FindAsync(guildId);
            if (guild == null) return NotFound();

            var existing = await _db.GuildMemberships.FirstOrDefaultAsync(m => m.GuildId == guildId && m.UserId == user.Id);
            if (existing != null)
            {
                return Ok(new { joined = false, message = "Bạn đã trong máy chủ này." });
            }

            _db.GuildMemberships.Add(new GuildMembership
            {
                GuildId = guildId,
                UserId = user.Id,
                Role = guild.OwnerId == user.Id ? "owner" : "member"
            });
            await _db.SaveChangesAsync();

            return Ok(new { joined = true });
        }

        [HttpGet("{guildId}/members")]
        public async Task<IActionResult> GetGuildMembers(int guildId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var membership = await _db.GuildMemberships
                .FirstOrDefaultAsync(m => m.GuildId == guildId && m.UserId == user.Id);
            if (membership == null)
            {
                return Forbid("Bạn không phải thành viên của máy chủ này.");
            }

            var memberIds = await _db.GuildMemberships
                .Where(m => m.GuildId == guildId)
                .Select(m => m.UserId)
                .ToListAsync();

            var members = await _db.Users
                .Where(u => memberIds.Contains(u.Id))
                .Select(u => new { 
                    u.Id, 
                    u.Username,
                    Role = _db.GuildMemberships
                        .Where(m => m.GuildId == guildId && m.UserId == u.Id)
                        .Select(m => m.Role)
                        .FirstOrDefault() ?? "member"
                })
                .ToListAsync();

            return Ok(members);
        }

        [HttpGet("{guildId}/channels/{channelId}/members")]
        public async Task<IActionResult> GetChannelMembers(int guildId, int channelId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var membership = await _db.GuildMemberships
                .FirstOrDefaultAsync(m => m.GuildId == guildId && m.UserId == user.Id);
            if (membership == null)
            {
                return Forbid("Bạn không phải thành viên của máy chủ này.");
            }

            var channel = await _db.Channels.FirstOrDefaultAsync(c => c.Id == channelId && c.GuildId == guildId);
            if (channel == null) return NotFound("Không tìm thấy kênh.");

            var memberIds = await _db.ChannelMemberships
                .Where(cm => cm.ChannelId == channelId)
                .Select(cm => cm.UserId)
                .ToListAsync();

            var members = await _db.Users
                .Where(u => memberIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Username })
                .ToListAsync();

            return Ok(members);
        }

        [HttpPost("{guildId}/invite")]
        public async Task<IActionResult> InviteFriendToGuild(int guildId, [FromBody] InviteFriendRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest("Username không được để trống.");
            }

            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var guild = await _db.Guilds.FindAsync(guildId);
            if (guild == null) return NotFound();

            var membership = await _db.GuildMemberships
                .FirstOrDefaultAsync(m => m.GuildId == guildId && m.UserId == user.Id);
            if (membership == null)
            {
                return Forbid("Bạn không phải thành viên của máy chủ này.");
            }

            var friend = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (friend == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            var areFriends = await _db.Friendships.AnyAsync(f =>
                (f.UserId == user.Id && f.FriendId == friend.Id) ||
                (f.UserId == friend.Id && f.FriendId == user.Id));
            if (!areFriends)
            {
                return BadRequest("Bạn chỉ có thể mời bạn bè vào máy chủ.");
            }

            var existingMember = await _db.GuildMemberships
                .FirstOrDefaultAsync(m => m.GuildId == guildId && m.UserId == friend.Id);
            if (existingMember != null)
            {
                return Conflict(new { message = "Người này đã là thành viên của máy chủ.", alreadyMember = true });
            }

            // Kiểm tra xem đã có invitation pending chưa
            var existingInvitation = await _db.GuildInvitations
                .FirstOrDefaultAsync(i => i.GuildId == guildId && i.InviteeId == friend.Id && i.Status == "Pending");
            if (existingInvitation != null)
            {
                return Conflict(new { message = "Đã gửi lời mời cho người này rồi.", alreadyInvited = true });
            }

            // Tạo invitation thay vì tự động thêm vào guild
            var invitation = new GuildInvitation
            {
                GuildId = guildId,
                InviterId = user.Id,
                InviteeId = friend.Id,
                Status = "Pending"
            };
            _db.GuildInvitations.Add(invitation);
            await _db.SaveChangesAsync();

            // Thông báo cho người được mời
            await _hubContext.Clients.All
                .SendAsync("GuildInvitationReceived", friend.Username, guildId, guild.Name, user.Username, invitation.Id);

            return Ok(new { success = true, message = $"Đã gửi lời mời đến {friend.Username}.", invitationId = invitation.Id });
        }

        [HttpPost("{guildId}/channels/{channelId}/invite")]
        public async Task<IActionResult> InviteFriendToChannel(int guildId, int channelId, [FromBody] InviteFriendRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest("Username không được để trống.");
            }

            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var guild = await _db.Guilds.FindAsync(guildId);
            if (guild == null) return NotFound();

            var channel = await _db.Channels.FirstOrDefaultAsync(c => c.Id == channelId && c.GuildId == guildId);
            if (channel == null) return NotFound("Không tìm thấy kênh.");

            var membership = await _db.GuildMemberships
                .FirstOrDefaultAsync(m => m.GuildId == guildId && m.UserId == user.Id);
            if (membership == null)
            {
                return Forbid("Bạn không phải thành viên của máy chủ này.");
            }

            // Kiểm tra user có quyền mời vào channel không (phải có membership trong channel hoặc là owner)
            var hasChannelAccess = await _db.ChannelMemberships
                .AnyAsync(cm => cm.ChannelId == channelId && cm.UserId == user.Id);
            if (!hasChannelAccess && guild.OwnerId != user.Id)
            {
                return Forbid("Bạn không có quyền mời vào kênh này.");
            }

            var friend = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (friend == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            // Kiểm tra bạn bè
            var areFriends = await _db.Friendships.AnyAsync(f =>
                (f.UserId == user.Id && f.FriendId == friend.Id) ||
                (f.UserId == friend.Id && f.FriendId == user.Id));
            if (!areFriends)
            {
                return BadRequest("Bạn chỉ có thể mời bạn bè vào kênh.");
            }

            // Kiểm tra friend đã là member của guild chưa
            var friendGuildMembership = await _db.GuildMemberships
                .FirstOrDefaultAsync(m => m.GuildId == guildId && m.UserId == friend.Id);
            if (friendGuildMembership == null)
            {
                return BadRequest("Người này chưa là thành viên của máy chủ. Hãy mời vào máy chủ trước.");
            }

            // Kiểm tra đã có trong channel chưa
            var existingChannelMember = await _db.ChannelMemberships
                .FirstOrDefaultAsync(cm => cm.ChannelId == channelId && cm.UserId == friend.Id);
            if (existingChannelMember != null)
            {
                return Conflict(new { message = "Người này đã có trong kênh này.", alreadyMember = true });
            }

            _db.ChannelMemberships.Add(new ChannelMembership
            {
                ChannelId = channelId,
                UserId = friend.Id
            });
            await _db.SaveChangesAsync();

            await _hubContext.Clients.Group($"guild:{guildId}")
                .SendAsync("ChannelMemberJoined", channelId, friend.Username);

            return Ok(new { success = true, message = $"Đã mời {friend.Username} vào kênh #{channel.Name}." });
        }

        [HttpDelete("{guildId}/members/{memberId}")]
        public async Task<IActionResult> RemoveGuildMember(int guildId, int memberId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var guild = await _db.Guilds.FindAsync(guildId);
            if (guild == null) return NotFound();

            var userMembership = await _db.GuildMemberships
                .FirstOrDefaultAsync(m => m.GuildId == guildId && m.UserId == user.Id);
            if (userMembership == null)
            {
                return Forbid("Bạn không phải thành viên của máy chủ này.");
            }

            // Chỉ owner hoặc admin mới có thể xóa thành viên
            if (guild.OwnerId != user.Id && userMembership.Role != "admin")
            {
                return Forbid("Chỉ chủ sở hữu hoặc quản trị viên mới có thể xóa thành viên.");
            }

            // Không thể xóa chính mình hoặc owner
            if (memberId == user.Id)
            {
                return BadRequest("Bạn không thể xóa chính mình.");
            }

            var memberMembership = await _db.GuildMemberships
                .FirstOrDefaultAsync(m => m.GuildId == guildId && m.UserId == memberId);
            if (memberMembership == null)
            {
                return NotFound("Không tìm thấy thành viên.");
            }

            if (guild.OwnerId == memberId)
            {
                return BadRequest("Không thể xóa chủ sở hữu máy chủ.");
            }

            // Xóa tất cả channel memberships của member
            var channelIds = await _db.Channels
                .Where(c => c.GuildId == guildId)
                .Select(c => c.Id)
                .ToListAsync();
            var channelMemberships = await _db.ChannelMemberships
                .Where(cm => channelIds.Contains(cm.ChannelId) && cm.UserId == memberId)
                .ToListAsync();
            _db.ChannelMemberships.RemoveRange(channelMemberships);

            _db.GuildMemberships.Remove(memberMembership);
            await _db.SaveChangesAsync();

            await _hubContext.Clients.Group($"guild:{guildId}")
                .SendAsync("GuildMemberRemoved", guildId, memberId);

            return NoContent();
        }

        [HttpPut("{guildId}/members/{memberId}/role")]
        public async Task<IActionResult> UpdateMemberRole(int guildId, int memberId, [FromBody] UpdateRoleRequest request)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var guild = await _db.Guilds.FindAsync(guildId);
            if (guild == null) return NotFound();

            // Chỉ owner mới có thể bổ nhiệm admin
            if (guild.OwnerId != user.Id)
            {
                return Forbid("Chỉ chủ sở hữu mới có thể bổ nhiệm quyền.");
            }

            var memberMembership = await _db.GuildMemberships
                .FirstOrDefaultAsync(m => m.GuildId == guildId && m.UserId == memberId);
            if (memberMembership == null)
            {
                return NotFound("Không tìm thấy thành viên.");
            }

            if (request.Role != "member" && request.Role != "admin")
            {
                return BadRequest("Role không hợp lệ.");
            }

            memberMembership.Role = request.Role;
            await _db.SaveChangesAsync();

            await _hubContext.Clients.Group($"guild:{guildId}")
                .SendAsync("GuildMemberRoleUpdated", guildId, memberId, request.Role);

            return Ok(new { success = true, message = $"Đã cập nhật quyền thành viên." });
        }

        public record InviteFriendRequest
        {
            public string Username { get; set; } = string.Empty;
        }

        private record GuildDto(int Id, string Name, string Description, int OwnerId, string Role, IEnumerable<ChannelDto> Channels);
        private record ChannelDto(int Id, string Name, string? Topic);

        public record CreateGuildRequest
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
        }

        [HttpPut("{guildId}/channels/{channelId}")]
        public async Task<IActionResult> UpdateChannel(int guildId, int channelId, [FromBody] UpdateChannelRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Tên kênh không được để trống.");
            }

            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var membership = await _db.GuildMemberships
                .FirstOrDefaultAsync(m => m.GuildId == guildId && m.UserId == user.Id);
            if (membership == null)
            {
                return Forbid("Bạn không phải thành viên của máy chủ này.");
            }

            // Chỉ owner hoặc admin mới có thể sửa kênh
            if (membership.Role != "owner" && membership.Role != "admin")
            {
                return Forbid("Chỉ chủ sở hữu hoặc quản trị viên mới có thể sửa kênh.");
            }

            var channel = await _db.Channels.FirstOrDefaultAsync(c => c.Id == channelId && c.GuildId == guildId);
            if (channel == null)
            {
                return NotFound("Không tìm thấy kênh.");
            }

            channel.Name = request.Name.Trim();
            channel.Topic = request.Topic?.Trim();
            await _db.SaveChangesAsync();

            var dto = new ChannelDto(channel.Id, channel.Name, channel.Topic);

            await _hubContext.Clients.Group($"guild:{guildId}")
                .SendAsync("GuildChannelUpdated", guildId, dto);

            return Ok(dto);
        }

        [HttpDelete("{guildId}/channels/{channelId}")]
        public async Task<IActionResult> DeleteChannel(int guildId, int channelId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var membership = await _db.GuildMemberships
                .FirstOrDefaultAsync(m => m.GuildId == guildId && m.UserId == user.Id);
            if (membership == null)
            {
                return Forbid("Bạn không phải thành viên của máy chủ này.");
            }

            // Chỉ owner hoặc admin mới có thể xóa kênh
            if (membership.Role != "owner" && membership.Role != "admin")
            {
                return Forbid("Chỉ chủ sở hữu hoặc quản trị viên mới có thể xóa kênh.");
            }

            var channel = await _db.Channels.FirstOrDefaultAsync(c => c.Id == channelId && c.GuildId == guildId);
            if (channel == null)
            {
                return NotFound("Không tìm thấy kênh.");
            }

            // Xóa tất cả tin nhắn trong kênh
            var messages = await _db.Messages.Where(m => m.ChannelId == channelId).ToListAsync();
            _db.Messages.RemoveRange(messages);

            _db.Channels.Remove(channel);
            await _db.SaveChangesAsync();

            await _hubContext.Clients.Group($"guild:{guildId}")
                .SendAsync("GuildChannelDeleted", guildId, channelId);

            return NoContent();
        }

        public record CreateChannelRequest
        {
            public string Name { get; set; } = string.Empty;
            public string? Topic { get; set; }
        }

        public record UpdateChannelRequest
        {
            public string Name { get; set; } = string.Empty;
            public string? Topic { get; set; }
        }

        public record UpdateRoleRequest
        {
            public string Role { get; set; } = "member";
        }

        [HttpGet("invitations")]
        public async Task<IActionResult> GetGuildInvitations()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var invitations = await _db.GuildInvitations
                .Where(i => i.InviteeId == user.Id && i.Status == "Pending")
                .Include(i => i.Guild)
                .Include(i => i.Inviter)
                .ToListAsync();

            var result = invitations.Select(i => new
            {
                i.Id,
                GuildId = i.Guild.Id,
                GuildName = i.Guild.Name,
                InviterUsername = i.Inviter.Username,
                i.CreatedAt
            }).ToList();

            return Ok(result);
        }

        [HttpPost("invitations/{invitationId}/accept")]
        public async Task<IActionResult> AcceptGuildInvitation(int invitationId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var invitation = await _db.GuildInvitations
                .Include(i => i.Guild)
                .FirstOrDefaultAsync(i => i.Id == invitationId && i.InviteeId == user.Id);

            if (invitation == null)
            {
                return NotFound("Không tìm thấy lời mời.");
            }

            if (invitation.Status != "Pending")
            {
                return BadRequest("Lời mời này đã được xử lý rồi.");
            }

            // Thêm vào guild
            var membership = new GuildMembership
            {
                GuildId = invitation.GuildId,
                UserId = user.Id,
                Role = "member"
            };
            _db.GuildMemberships.Add(membership);

            // Tự động thêm vào channel "general"
            var generalChannel = await _db.Channels
                .FirstOrDefaultAsync(c => c.GuildId == invitation.GuildId && c.Name == "general");
            if (generalChannel != null)
            {
                _db.ChannelMemberships.Add(new ChannelMembership
                {
                    ChannelId = generalChannel.Id,
                    UserId = user.Id
                });
            }

            // Cập nhật invitation
            invitation.Status = "Accepted";
            invitation.RespondedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Thông báo cho guild
            await _hubContext.Clients.Group($"guild:{invitation.GuildId}")
                .SendAsync("GuildMemberJoined", invitation.GuildId, user.Username);

            // Thông báo cho user
            await _hubContext.Clients.All
                .SendAsync("GuildInvitationAccepted", user.Username, invitation.GuildId, invitation.Guild.Name);

            return Ok(new { success = true, message = $"Đã tham gia máy chủ {invitation.Guild.Name}." });
        }

        [HttpPost("invitations/{invitationId}/reject")]
        public async Task<IActionResult> RejectGuildInvitation(int invitationId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var invitation = await _db.GuildInvitations
                .Include(i => i.Guild)
                .FirstOrDefaultAsync(i => i.Id == invitationId && i.InviteeId == user.Id);

            if (invitation == null)
            {
                return NotFound("Không tìm thấy lời mời.");
            }

            if (invitation.Status != "Pending")
            {
                return BadRequest("Lời mời này đã được xử lý rồi.");
            }

            invitation.Status = "Rejected";
            invitation.RespondedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã từ chối lời mời." });
        }
    }
}

