using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ChatApp.Data;
using ChatApp.Models;
using ChatApp.Services;
using ChatApp.Hubs;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ChatApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly MessageRepository _repo;
        private readonly AppDbContext _db;
        private readonly AuthService _authService;
        private readonly IHubContext<ChatHub> _hubContext;

        public MessageController(MessageRepository repo, AppDbContext db, AuthService authService, IHubContext<ChatHub> hubContext)
        {
            _repo = repo;
            _db = db;
            _authService = authService;
            _hubContext = hubContext;
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var header))
                return null;

            var token = header.ToString();
            if (token.StartsWith("Bearer ", System.StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring("Bearer ".Length);
            }

            if (string.IsNullOrWhiteSpace(token))
                return null;

            return await _authService.GetUserFromTokenAsync(token);
        }

        [HttpGet]
        public async Task<IActionResult> GetRecent([FromQuery] int? channelId, [FromQuery] int limit = 200)
        {
            if (channelId.HasValue)
            {
                var channelMessages = await _repo.GetChannelMessagesAsync(channelId.Value, limit);
                return Ok(channelMessages);
            }

            var msgs = await _repo.GetRecentMessagesAsync(limit);
            return Ok(msgs);
        }

        [HttpGet("conversation")]
        public async Task<IActionResult> GetConversation([FromQuery] string userA, [FromQuery] string userB, [FromQuery] int limit = 200)
        {
            if (string.IsNullOrWhiteSpace(userA) || string.IsNullOrWhiteSpace(userB))
            {
                return BadRequest("Missing participants.");
            }

            var convo = await _repo.GetConversationAsync(userA, userB, limit);
            return Ok(convo);
        }

        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Unauthorized();

            var message = await _db.Messages.FindAsync(messageId);
            if (message == null) return NotFound();

            if (message.Sender != user.Username)
            {
                return Forbid("Bạn chỉ có thể xóa tin nhắn của chính mình.");
            }

            var channelId = message.ChannelId;
            var recipient = message.Recipient;
            var sender = message.Sender;

            _db.Messages.Remove(message);
            await _db.SaveChangesAsync();

            if (channelId.HasValue)
            {
                await _hubContext.Clients.Group($"channel:{channelId.Value}")
                    .SendAsync("MessageDeleted", messageId, channelId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(recipient) && !string.IsNullOrWhiteSpace(sender))
            {
                var group = string.CompareOrdinal(sender, recipient) < 0
                    ? $"dm:{sender}:{recipient}"
                    : $"dm:{recipient}:{sender}";
                await _hubContext.Clients.Group(group)
                    .SendAsync("MessageDeleted", messageId, null);
            }

            return NoContent();
        }
    }
}
