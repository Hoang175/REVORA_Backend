namespace REVORA_BE.DTOs.Request
{
    public class CreateCommentRequestDto
    {
        public string Content { get; set; } = null!;
        public long? ParentId { get; set; }
    }
}