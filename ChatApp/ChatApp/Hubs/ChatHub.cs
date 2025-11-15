using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatApp.Models;
using ChatApp.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Hubs
{
    public class ChatHub : Hub
    {
        // Mapping connectionId -> username
        private static readonly ConcurrentDictionary<string, string> ConnectionUser = new();

        // For convenience, inject IServiceScopeFactory to get scoped DbContext
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IServiceScopeFactory scopeFactory, ILogger<ChatHub> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        private static IEnumerable<string> GetConnections(string username)
        {
            return ConnectionUser
                .Where(kvp => kvp.Value.Equals(username, StringComparison.OrdinalIgnoreCase))
                .Select(kvp => kvp.Key);
        }

        private static string GetDirectGroup(string userA, string userB)
        {
            return string.CompareOrdinal(userA, userB) < 0
                ? $"dm:{userA}:{userB}"
                : $"dm:{userB}:{userA}";
        }

        private static string ChannelGroup(int channelId) => $"channel:{channelId}";
        private static string GuildGroup(int guildId) => $"guild:{guildId}";

        private string? GetUsername()
        {
            return ConnectionUser.TryGetValue(Context.ConnectionId, out var username)
                ? username
                : null;
        }

        // Client should call RegisterUser(username) once connected
        public async Task RegisterUser(string username)
        {
            ConnectionUser[Context.ConnectionId] = username;
            await BroadcastUserList();
            await Clients.Others.SendAsync("UserConnected", username);
            _logger.LogInformation("{User} connected (conn:{Conn})", username, Context.ConnectionId);
        }

        public async Task JoinGuildGroup(int guildId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GuildGroup(guildId));
        }

        // Sending text message: save then broadcast
        public async Task SendMessage(string sender, string message)
        {
            var username = GetUsername() ?? sender;
            var msg = new Message
            {
                Sender = username,
                Content = message,
                Recipient = null,
                Type = "text",
                Timestamp = DateTime.UtcNow
            };

            // Save to DB
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ChatApp.Data.AppDbContext>();
                db.Messages.Add(msg);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving message");
            }

            // Broadcast
            await Clients.All.SendAsync("ReceiveMessage", username, message, msg.Timestamp);
        }

        // Sending sticker
        public async Task SendSticker(string sender, string stickerUrl)
        {
            var username = GetUsername() ?? sender;
            var msg = new Message
            {
                Sender = username,
                MediaUrl = stickerUrl,
                Recipient = null,
                Type = "sticker",
                Timestamp = DateTime.UtcNow
            };

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ChatApp.Data.AppDbContext>();
                db.Messages.Add(msg);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving sticker");
            }

            await Clients.All.SendAsync("ReceiveSticker", username, stickerUrl, msg.Timestamp);
        }

        public async Task JoinChannel(int channelId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ChannelGroup(channelId));
        }

        public async Task LeaveChannel(int channelId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ChannelGroup(channelId));
        }

        public async Task SendChannelMessage(int channelId, string message)
        {
            var username = GetUsername();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(message))
                return;

            // Kiểm tra user có quyền gửi tin nhắn trong channel không
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null) return;

                var channel = await db.Channels.FindAsync(channelId);
                if (channel == null) return;

                // Kiểm tra channel membership hoặc là owner của guild
                var hasAccess = await db.ChannelMemberships
                    .AnyAsync(cm => cm.ChannelId == channelId && cm.UserId == user.Id);
                
                if (!hasAccess)
                {
                    var guild = await db.Guilds.FindAsync(channel.GuildId);
                    if (guild == null || guild.OwnerId != user.Id)
                    {
                        await Clients.Caller.SendAsync("Error", "Bạn không có quyền gửi tin nhắn trong kênh này.");
                        return;
                    }
                }

                var msg = new Message
                {
                    Sender = username,
                    Content = message,
                    ChannelId = channelId,
                    Type = "text",
                    Timestamp = DateTime.UtcNow
                };

                db.Messages.Add(msg);
                await db.SaveChangesAsync();

                await Clients.Group(ChannelGroup(channelId))
                    .SendAsync("ReceiveChannelMessage", channelId, username, message, msg.Timestamp, msg.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving channel message");
            }
        }

        public async Task SendChannelSticker(int channelId, string stickerUrl)
        {
            var username = GetUsername();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(stickerUrl))
                return;

            var msg = new Message
            {
                Sender = username,
                MediaUrl = stickerUrl,
                ChannelId = channelId,
                Type = "sticker",
                Timestamp = DateTime.UtcNow
            };

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Messages.Add(msg);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving channel sticker");
            }

            await Clients.Group(ChannelGroup(channelId))
                .SendAsync("ReceiveChannelSticker", channelId, username, stickerUrl, msg.Timestamp);
        }

        public async Task OpenDirectChannel(string requester, string peer)
        {
            if (string.IsNullOrWhiteSpace(requester) || string.IsNullOrWhiteSpace(peer))
                return;

            var group = GetDirectGroup(requester, peer);
            await Groups.AddToGroupAsync(Context.ConnectionId, group);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<MessageRepository>();
                var history = await repo.GetConversationAsync(requester, peer);
                await Clients.Caller.SendAsync("DirectHistory", peer, history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading DM history for {Requester} and {Peer}", requester, peer);
            }
        }

        public async Task SendDirectMessage(string sender, string recipient, string message)
        {
            var username = GetUsername() ?? sender;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(recipient) || string.IsNullOrWhiteSpace(message))
                return;

            Message? msg = null;
            DateTime payloadTime = DateTime.UtcNow;

            // Kiểm tra xem 2 user đã là bạn bè chưa
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                // Tìm user trong database
                var senderUser = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
                var recipientUser = await db.Users.FirstOrDefaultAsync(u => u.Username == recipient);
                
                if (senderUser == null || recipientUser == null)
                {
                    _logger.LogWarning("User not found: {Sender} or {Recipient}", username, recipient);
                    await Clients.Caller.SendAsync("Error", "Người dùng không tồn tại.");
                    return;
                }

                // Kiểm tra bạn bè (kiểm tra cả 2 chiều)
                var areFriends = await db.Friendships.AnyAsync(f => 
                    (f.UserId == senderUser.Id && f.FriendId == recipientUser.Id) ||
                    (f.UserId == recipientUser.Id && f.FriendId == senderUser.Id));

                if (!areFriends)
                {
                    _logger.LogWarning("Users are not friends: {Sender} -> {Recipient}", username, recipient);
                    await Clients.Caller.SendAsync("Error", "Bạn cần kết bạn với người này trước khi chat.");
                    return;
                }

                msg = new Message
                {
                    Sender = username,
                    Content = message,
                    Type = "dm",
                    Recipient = recipient,
                    Timestamp = DateTime.UtcNow
                };

                db.Messages.Add(msg);
                await db.SaveChangesAsync();
                payloadTime = msg.Timestamp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving direct message");
                await Clients.Caller.SendAsync("Error", "Không thể gửi tin nhắn.");
                return;
            }

            if (msg == null) return;

            var allTargets = GetConnections(recipient)
                .Concat(GetConnections(username))
                .Distinct()
                .ToList();

            if (allTargets.Count > 0)
            {
                await Clients.Clients(allTargets)
                    .SendAsync("ReceiveDirectMessage", username, recipient, message, payloadTime, msg.Id);
            }
        }

        public async Task SendDirectAttachment(string sender, string recipient, string mediaUrl, string? fileName = null)
        {
            var username = GetUsername() ?? sender;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(recipient) || string.IsNullOrWhiteSpace(mediaUrl))
                return;

            Message? msg = null;
            DateTime payloadTime = DateTime.UtcNow;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var senderUser = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
                var recipientUser = await db.Users.FirstOrDefaultAsync(u => u.Username == recipient);

                if (senderUser == null || recipientUser == null)
                {
                    _logger.LogWarning("User not found (attachment): {Sender} or {Recipient}", username, recipient);
                    await Clients.Caller.SendAsync("Error", "Người dùng không tồn tại.");
                    return;
                }

                var areFriends = await db.Friendships.AnyAsync(f =>
                    (f.UserId == senderUser.Id && f.FriendId == recipientUser.Id) ||
                    (f.UserId == recipientUser.Id && f.FriendId == senderUser.Id));

                if (!areFriends)
                {
                    _logger.LogWarning("Users are not friends (attachment): {Sender} -> {Recipient}", username, recipient);
                    await Clients.Caller.SendAsync("Error", "Bạn cần kết bạn với người này trước khi gửi tệp.");
                    return;
                }

                msg = new Message
                {
                    Sender = username,
                    Recipient = recipient,
                    MediaUrl = mediaUrl,
                    Content = fileName,
                    Type = "dm",
                    Timestamp = DateTime.UtcNow
                };

                db.Messages.Add(msg);
                await db.SaveChangesAsync();
                payloadTime = msg.Timestamp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving direct attachment");
                await Clients.Caller.SendAsync("Error", "Không thể gửi tệp.");
                return;
            }

            if (msg == null) return;

            var targets = GetConnections(recipient)
                .Concat(GetConnections(username))
                .Distinct()
                .ToList();

            if (targets.Count > 0)
            {
                await Clients.Clients(targets)
                    .SendAsync("ReceiveDirectAttachment", username, recipient, mediaUrl, fileName, payloadTime);
            }
        }

        // Typing indicator - broadcast to others with context
        public async Task Typing(string username, string? contextType = null, int? channelId = null, string? recipient = null)
        {
            if (!string.IsNullOrWhiteSpace(contextType))
            {
                var context = new Dictionary<string, object>();
                context["type"] = contextType;
                if (contextType == "channel" && channelId.HasValue)
                {
                    context["channelId"] = channelId.Value;
                    await Clients.Group(ChannelGroup(channelId.Value))
                        .SendAsync("UserTyping", username, context);
                }
                else if (contextType == "dm" && !string.IsNullOrWhiteSpace(recipient))
                {
                    context["recipient"] = recipient;
                    // Gửi đến cả sender và recipient
                    var targets = GetConnections(recipient)
                        .Concat(GetConnections(username))
                        .Distinct()
                        .ToList();
                    if (targets.Count > 0)
                    {
                        await Clients.Clients(targets)
                            .SendAsync("UserTyping", username, context);
                    }
                }
            }
            else
            {
                await Clients.Others.SendAsync("UserTyping", username);
            }
        }

        public async Task StopTyping(string username, string? contextType = null, int? channelId = null, string? recipient = null)
        {
            if (!string.IsNullOrWhiteSpace(contextType))
            {
                var context = new Dictionary<string, object>();
                context["type"] = contextType;
                if (contextType == "channel" && channelId.HasValue)
                {
                    context["channelId"] = channelId.Value;
                    await Clients.Group(ChannelGroup(channelId.Value))
                        .SendAsync("UserStopTyping", context);
                }
                else if (contextType == "dm" && !string.IsNullOrWhiteSpace(recipient))
                {
                    context["recipient"] = recipient;
                    var targets = GetConnections(recipient)
                        .Concat(GetConnections(username))
                        .Distinct()
                        .ToList();
                    if (targets.Count > 0)
                    {
                        await Clients.Clients(targets)
                            .SendAsync("UserStopTyping", context);
                    }
                }
            }
            else
            {
                await Clients.Others.SendAsync("UserStopTyping");
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (ConnectionUser.TryRemove(Context.ConnectionId, out var username))
            {
                await BroadcastUserList();
                await Clients.Others.SendAsync("UserDisconnected", username);
                _logger.LogInformation("{User} disconnected (conn:{Conn})", username, Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        private async Task BroadcastUserList()
        {
            // Distinct usernames online
            var users = ConnectionUser.Values.Distinct().OrderBy(u => u).ToList();
            await Clients.All.SendAsync("UserList", users);
        }
    }
}
