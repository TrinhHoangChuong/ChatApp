namespace ChatApp.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int SenderId { get; set; } // User ID who sent the message
        public string Sender { get; set; } = string.Empty; // Username for backward compatibility
        public int? RoomId { get; set; } // null = DM, not null = room message
        public int? ReceiverId { get; set; } // For DM: target user ID
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User? SenderUser { get; set; }
        public virtual Room? Room { get; set; }
        public virtual User? Receiver { get; set; }
    }
}
