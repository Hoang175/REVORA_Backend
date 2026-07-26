using REVORA_BE.DTOs;
using REVORA_BE.DTOs.Request;
using REVORA_BE.DTOs.Response;

namespace REVORA_BE.Services.Interfaces
{
    public interface IFeedbackService
    {
        Task<FeedbackResponseDto> SubmitFeedbackAsync(long? userId, FeedbackRequestDto dto, CancellationToken cancellationToken = default);
        Task<PagedResult<FeedbackResponseDto>> GetAllFeedbacksAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task UpdateFeedbackStatusAsync(long feedbackId, string status, CancellationToken cancellationToken = default);
    }
}
