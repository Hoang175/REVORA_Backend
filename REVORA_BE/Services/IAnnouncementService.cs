using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using REVORA_BE.DTOs;

namespace REVORA_BE.Services
{
    public interface IAnnouncementService
    {
        Task<List<AnnouncementResponseDto>> GetActiveAnnouncementsAsync(CancellationToken cancellationToken = default);
        Task<List<AnnouncementResponseDto>> GetAllAnnouncementsAsync(CancellationToken cancellationToken = default);
        Task<AnnouncementResponseDto> CreateAnnouncementAsync(REVORA_BE.DTOs.Request.AnnouncementCreateDto request, CancellationToken cancellationToken = default);
        Task<AnnouncementResponseDto> UpdateAnnouncementAsync(long id, REVORA_BE.DTOs.Request.AnnouncementUpdateDto request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAnnouncementAsync(long id, CancellationToken cancellationToken = default);
    }
}
