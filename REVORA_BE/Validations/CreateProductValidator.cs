using FluentValidation;
using REVORA_BE.DTOs.Request;

namespace REVORA_BE.Validations
{
    public class CreateProductValidator : AbstractValidator<CreateProductRequestDto>
    {
        public CreateProductValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("Tên sản phẩm không được để trống.");
            RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("Vui lòng chọn danh mục.");
            RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Giá sản phẩm không hợp lệ.");
            RuleFor(x => x.ImageUrls).Must(x => x != null && x.Count >= 1)
                                     .WithMessage("Vui lòng tải lên ít nhất 1 hình ảnh.");

            // Validate Premium Features
            When(x => x.EnableVideoUpload, () => {
                RuleFor(x => x.VideoUrl).NotEmpty().WithMessage("Vui lòng đính kèm Video URL khi chọn tính năng Video Shorts.");
            });

            When(x => x.EnableBannerBoost, () => {
                RuleFor(x => x.BannerUrl).NotEmpty().WithMessage("Vui lòng đính kèm Banner URL khi chọn tính năng Banner Nổi Bật.");
            });
        }
    }
}