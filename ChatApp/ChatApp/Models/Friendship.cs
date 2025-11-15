using System;
using System.ComponentModel.DataAnnotations;

namespace ChatApp.Models
{
    public class Friendship
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int FriendId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

