using System.Collections.Generic;

namespace REVORA_BE.DTOs.Request
{
    public class StartMatchSessionRequestDto
    {
        public List<long> ProductIds { get; set; } = new();
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public string? City { get; set; }
    }
}
