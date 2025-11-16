using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatApp.Models
{
    [Table("Messages")]
    public class Message
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Người gửi (username)
        [Required]
        [MaxLength(100)]
        public string Sender { get; set; } = string.Empty;

        // Nội dung text (null nếu là sticker/file)
        public string? Content { get; set; }

        // Nếu là sticker/file, lưu url vào đây
        public string? MediaUrl { get; set; }

        // Nếu là tin nhắn riêng tư, lưu người nhận
        public string? Recipient { get; set; }

        // Nếu là tin nhắn trong kênh, lưu ChannelId
        public int? ChannelId { get; set; }

        // Nếu là tin nhắn trong room, lưu RoomId
        public int? RoomId { get; set; }

        // Loại message: "text", "sticker", "file"
        [Required]
        public string Type { get; set; } = "text";

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
