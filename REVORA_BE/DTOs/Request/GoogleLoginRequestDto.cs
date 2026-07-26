using System.ComponentModel.DataAnnotations;

namespace REVORA_BE.DTOs.Request
{
    public class GoogleLoginRequestDto
    {
        [Required(ErrorMessage = "IdToken is required.")]
        public string IdToken { get; set; } = null!;
    }
}
