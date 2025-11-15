using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Data
{
    public class MessageRepository
    {
        private readonly AppDbContext _context;
        public MessageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Message> AddMessageAsync(Message msg)
        {
            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();
            return msg;
        }

        public async Task<List<Message>> GetAllMessagesAsync()
        {
            return await _context.Messages.OrderBy(m => m.Timestamp).ToListAsync();
        }

        // Lấy messages gần đây (limit)
        public async Task<List<Message>> GetRecentMessagesAsync(int limit = 100)
        {
            return await _context.Messages
                           .Where(m => m.Type != "dm" && m.ChannelId == null)
                           .OrderByDescending(m => m.Timestamp)
                           .Take(limit)
                           .OrderBy(m => m.Timestamp)
                           .ToListAsync();
        }

        public async Task<List<Message>> GetChannelMessagesAsync(int channelId, int limit = 200)
        {
            return await _context.Messages
                .Where(m => m.ChannelId == channelId)
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        // Lấy hội thoại trực tiếp giữa 2 user
        public async Task<List<Message>> GetConversationAsync(string userA, string userB, int limit = 200)
        {
            return await _context.Messages
                .Where(m =>
                    m.Type == "dm" &&
                    (
                        (m.Sender == userA && m.Recipient == userB) ||
                        (m.Sender == userB && m.Recipient == userA)
                    ))
                .OrderBy(m => m.Timestamp)
                .Take(limit)
                .ToListAsync();
        }
    }
}
