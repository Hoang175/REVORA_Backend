using System;

namespace REVORA_BE.DTOs.Response
{
    public class CommentResponseDto
    {
        public long CommentId { get; set; }
        public long UserId { get; set; }
        public long? ParentId { get; set; }
        public string FullName { get; set; } = null!;
        public string AvatarUrl { get; set; } = null!;
        public string Content { get; set; } = null!;
        public int LikeCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UserBadgeName { get; set; }
    }
}