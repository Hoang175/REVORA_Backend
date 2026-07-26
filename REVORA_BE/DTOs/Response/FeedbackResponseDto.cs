using System;

namespace REVORA_BE.DTOs.Response
{
    public class FeedbackResponseDto
    {
        public long FeedbackId { get; set; }
        public long? UserId { get; set; }
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
