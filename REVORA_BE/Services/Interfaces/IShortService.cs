using REVORA_BE.DTOs.Request;
using REVORA_BE.DTOs.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Interfaces
{
    public interface IShortService
    {
        Task<List<ShortResponseDto>> GetFeedShortsAsync(long? currentUserId);
        Task<bool> ToggleLikeAsync(long userId, long shortId);
        Task<List<ShortCommentResponseDto>> GetShortCommentsAsync(long shortId, long? currentUserId);
        Task<ShortCommentResponseDto> AddCommentAsync(long userId, long shortId, CreateCommentRequestDto request);
        Task<ShortCommentResponseDto?> UpdateCommentAsync(long userId, long commentId, UpdateCommentRequestDto request);
        Task<bool> DeleteCommentAsync(long userId, long commentId);
        Task<bool> ToggleLikeCommentAsync(long userId, long commentId);
        Task<bool> ChangeShortStatusAsync(long userId, long shortId, string status);
    }
}