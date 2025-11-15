using System.Collections.Generic;
using System.Threading.Tasks;
using ChatApp.Data;
using ChatApp.Models;

namespace ChatApp.Services
{
    public class MessageService
    {
        private readonly MessageRepository _messageRepo;

        public MessageService(MessageRepository messageRepo)
        {
            _messageRepo = messageRepo;
        }

        public Task<Message> AddMessageAsync(Message message)
        {
            return _messageRepo.AddMessageAsync(message);
        }

        public Task<List<Message>> GetRecentMessagesAsync(int limit = 100)
        {
            return _messageRepo.GetRecentMessagesAsync(limit);
        }

        public Task<List<Message>> GetConversationAsync(string userA, string userB, int limit = 200)
        {
            return _messageRepo.GetConversationAsync(userA, userB, limit);
        }

        public Task<List<Message>> GetAllMessagesAsync()
        {
            return _messageRepo.GetAllMessagesAsync();
        }
    }
}
