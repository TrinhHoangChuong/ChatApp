namespace ChatApp.Models
{
    public class Room
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsPrivate { get; set; } = false; // false = public room, true = private DM
        public int? CreatorId { get; set; } // User who created the room
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<RoomMember> Members { get; set; } = new List<RoomMember>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}

