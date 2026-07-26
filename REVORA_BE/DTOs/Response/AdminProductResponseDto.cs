using System;
using System.Collections.Generic;

namespace REVORA_BE.DTOs.Response
{
    public class AdminProductResponseDto
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Price { get; set; }
        public string Category { get; set; } = null!;
        public List<string> Images { get; set; } = new List<string>();
        public AdminPostOwnerDto Owner { get; set; } = new AdminPostOwnerDto();
        public string CreatedAt { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int Views { get; set; }
        public int ContactCount { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsFeatured { get; set; }
        public string Condition { get; set; } = null!;
        public string Size { get; set; } = null!;
        public string Brand { get; set; } = null!;
    }

    public class AdminPostOwnerDto
    {
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Avatar { get; set; } = null!;
    }
}
