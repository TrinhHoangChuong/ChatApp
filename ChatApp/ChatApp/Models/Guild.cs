using System;
using System.ComponentModel.DataAnnotations;

namespace ChatApp.Models
{
    public class Guild
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }

        [Required]
        public int OwnerId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

