using REVORA_BE.DTOs.Response;
using REVORA_BE.Models;
using REVORA_BE.Repositories.Interfaces;
using REVORA_BE.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Implementations
{
    public class PaidCreditPackageService : IPaidCreditPackageService
    {
        private readonly IPaidCreditPackageRepository _repository;

        public PaidCreditPackageService(
            IPaidCreditPackageRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<PaidCreditPackageResponseDto>> GetAllActivePackagesAsync()
        {
            var packages = await _repository.GetAllActivePackagesAsync();
            var result = new List<PaidCreditPackageResponseDto>();

            foreach (var package in packages)
                result.Add(MapToDto(package));

            return result;
        }

        public async Task<PaidCreditPackageResponseDto?> GetPackageByIdAsync(long id)
        {
            var package = await _repository.GetByIdAsync(id);
            if (package == null) return null;

            return MapToDto(package);
        }

        private PaidCreditPackageResponseDto MapToDto(PaidCreditPackage p)
        {
            return new PaidCreditPackageResponseDto
            {
                PaidCreditPackageId = p.PaidCreditPackageId,
                Name = p.Name,
                CreditTypeId = p.CreditTypeId,
                CreditTypeName = p.CreditType?.Name,
                CreditAmount = p.CreditAmount,
                DurationDays = p.DurationDays,
                OriginalPrice = p.OriginalPrice,
                DiscountRate = p.DiscountRate,
                DiscountedPrice = p.DiscountedPrice,
                RewardBadgeId = p.RewardBadgeId,
                RewardBadge = p.RewardBadge == null ? null : new BadgeResponseDto
                {
                    BadgeId = p.RewardBadge.BadgeId,
                    Name = p.RewardBadge.Name,
                    IconUrl = p.RewardBadge.IconUrl,
                    Description = p.RewardBadge.Description
                },
                IsActive = p.IsActive,
                Descriptions = p.Descriptions?.OrderBy(d => d.DisplayOrder).Select(d => d.Content).ToList() ?? new List<string>()
            };
        }

        public async Task<bool> UpdatePackageAsync(long id, REVORA_BE.DTOs.Request.AdminUpdatePackageRequestDto request)
        {
            var package = await _repository.GetByIdAsync(id);
            if (package == null) return false;

            package.Name = request.Name;
            package.OriginalPrice = request.OriginalPrice;
            package.DiscountRate = request.DiscountRate;
            package.DiscountedPrice = request.DiscountedPrice;
            package.IsActive = request.IsActive;
            package.RewardBadgeId = request.RewardBadgeId;

            if (package.Descriptions != null)
            {
                package.Descriptions.Clear();
            }
            else
            {
                package.Descriptions = new List<PaidCreditPackageDescription>();
            }

            int order = 1;
            foreach (var desc in request.Descriptions)
            {
                if (!string.IsNullOrWhiteSpace(desc))
                {
                    package.Descriptions.Add(new PaidCreditPackageDescription
                    {
                        Content = desc,
                        DisplayOrder = order++
                    });
                }
            }

            await _repository.UpdateAsync(package);
            return true;
        }
    }
}
