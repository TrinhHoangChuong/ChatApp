namespace ChatApp.Models
{
    public class RoomMember
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public int UserId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Room? Room { get; set; }
        public virtual User? User { get; set; }
    }
}

