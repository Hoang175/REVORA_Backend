using System;

namespace REVORA_BE.DTOs.Response
{
    public class NotificationResponseDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Time { get; set; } = null!;
        public bool Read { get; set; }
        public string? ReferenceId { get; set; }
    }
}
