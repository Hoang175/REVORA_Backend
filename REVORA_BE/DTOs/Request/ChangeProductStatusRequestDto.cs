using System.ComponentModel.DataAnnotations;

namespace REVORA_BE.DTOs.Request
{
    public class ChangeProductStatusRequestDto
    {
        [Required]
        public string Status { get; set; } = null!;
    }
}
