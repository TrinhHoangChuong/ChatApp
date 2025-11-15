using System;
using System.ComponentModel.DataAnnotations;

namespace ChatApp.Models
{
    public class GuildInvitation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GuildId { get; set; }

        [Required]
        public int InviterId { get; set; }

        [Required]
        public int InviteeId { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RespondedAt { get; set; }

        // Navigation properties
        public Guild Guild { get; set; }
        public User Inviter { get; set; }
        public User Invitee { get; set; }
    }
}

