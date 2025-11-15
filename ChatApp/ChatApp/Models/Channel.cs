using System;
using System.ComponentModel.DataAnnotations;

namespace ChatApp.Models
{
    public class Channel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GuildId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Topic { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

