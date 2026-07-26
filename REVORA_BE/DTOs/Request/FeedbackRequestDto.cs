using System.ComponentModel.DataAnnotations;

namespace REVORA_BE.DTOs.Request
{
    public class FeedbackRequestDto
    {
        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung ý kiến là bắt buộc.")]
        [StringLength(2000, ErrorMessage = "Nội dung không được vượt quá 2000 ký tự.")]
        public string Message { get; set; } = string.Empty;
    }
}
