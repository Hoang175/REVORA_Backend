using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("Messages")]
    public class Message
    {
        public long MessageId { get; set; }
        public long ConversationId { get; set; }
        public long SenderId { get; set; }
        public string? Content { get; set; }
        public string? AttachmentUrl { get; set; }
        public long? ProductRefId { get; set; }
        public string Source { get; set; } = "CHAT";
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsRevoked { get; set; }
        public bool IsEdited { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Conversation? Conversation { get; set; }
        public User? Sender { get; set; }
        public Product? ProductRef { get; set; }
    }
}