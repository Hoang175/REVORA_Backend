using System.Collections.Generic;

namespace REVORA_BE.DTOs.Request
{
    public class MatchNegotiateRequestDto
    {
        public List<long> SelectedProductIds { get; set; } = new();
    }
}
