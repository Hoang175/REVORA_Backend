using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("Conversations")]
    public class Conversation
    {
        public long ConversationId { get; set; }
        public long User1Id { get; set; }
        public long User2Id { get; set; }
        public DateTime? DeletedAtUser1 { get; set; }
        public DateTime? DeletedAtUser2 { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User1 { get; set; }
        public User? User2 { get; set; }
        public ICollection<Message> Messages { get; set; } = new HashSet<Message>();
    }
}