using System;
using System.ComponentModel.DataAnnotations;

namespace ChatApp.Models
{
    public class ChannelMembership
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChannelId { get; set; }

        [Required]
        public int UserId { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}

