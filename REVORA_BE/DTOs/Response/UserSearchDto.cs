namespace REVORA_BE.DTOs.Response
{
    public class UserSearchDto
    {
        public long Id { get; set; }
        public string Username { get; set; } = null!;
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string Email { get; set; } = null!;
    }
}
