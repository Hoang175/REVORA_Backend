namespace REVORA_BE.DTOs.Request
{
    public class ProductFilterRequestDto
    {
        public string? Keyword { get; set; }
        public int? CategoryId { get; set; }
        public string? Brand { get; set; }
        public string? Condition { get; set; }
        public string? City { get; set; } // Khu vực
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? SortBy { get; set; } // Mặc định: "Newest", có thể là "PriceAsc", "PriceDesc"

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}