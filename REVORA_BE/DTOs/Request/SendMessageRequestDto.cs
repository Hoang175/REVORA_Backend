namespace REVORA_BE.DTOs.Request
{
    public class SendMessageRequestDto
    {
        public long ReceiverId { get; set; }
        public string? Content { get; set; }
        public string? AttachmentUrl { get; set; }
        public long? ProductRefId { get; set; }
    }
}