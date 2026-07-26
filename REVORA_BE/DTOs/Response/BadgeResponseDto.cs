namespace REVORA_BE.DTOs.Response
{
    public class BadgeResponseDto
    {
        public int BadgeId { get; set; }
        public string Name { get; set; } = null!;
        public string IconUrl { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsOwned { get; set; }
        public DateTime? ExpiredAt { get; set; }
    }
}
