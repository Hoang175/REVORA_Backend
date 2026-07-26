using System.Collections.Generic;

namespace REVORA_BE.DTOs.Request
{
    public class CreateProductRequestDto
    {
        public int CategoryId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? Brand { get; set; }
        public string? Condition { get; set; }

        // Danh sách link ảnh đã được upload lên Cloud (S3, Cloudinary...)
        public List<string> ImageUrls { get; set; } = new();

        // Premium Features
        public bool EnableVideoUpload { get; set; }
        public string? VideoUrl { get; set; }

        public bool EnableBannerBoost { get; set; }
        public string? BannerUrl { get; set; }
    }


}