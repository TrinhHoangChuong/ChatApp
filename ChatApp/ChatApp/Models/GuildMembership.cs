using System;
using System.ComponentModel.DataAnnotations;

namespace ChatApp.Models
{
    public class GuildMembership
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GuildId { get; set; }

        [Required]
        public int UserId { get; set; }

        [MaxLength(50)]
        public string Role { get; set; } = "member";

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}

