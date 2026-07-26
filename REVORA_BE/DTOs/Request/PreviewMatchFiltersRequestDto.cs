namespace REVORA_BE.DTOs.Request
{
    public class PreviewMatchFiltersRequestDto
    {
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public string? City { get; set; }
    }
}
