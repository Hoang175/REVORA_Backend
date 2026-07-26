using System.Collections.Generic;

namespace REVORA_BE.DTOs.Request
{
    public class MatchBulkSwipeRequestDto
    {
        public List<long> ProductIds { get; set; } = new();
        public long TargetUserId { get; set; }
    }
}
