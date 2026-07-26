using Microsoft.EntityFrameworkCore;
using REVORA_BE.DTOs;
using REVORA_BE.DTOs.Request;
using REVORA_BE.DTOs.Response;
using REVORA_BE.Exceptions;
using REVORA_BE.Models;
using REVORA_BE.Services.Interfaces;

namespace REVORA_BE.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly AppDbContext _context;

        public FeedbackService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<FeedbackResponseDto> SubmitFeedbackAsync(long? userId, FeedbackRequestDto dto, CancellationToken cancellationToken = default)
        {
            var feedback = new Feedback
            {
                UserId = userId,
                Email = dto.Email,
                Message = dto.Message,
                Status = "New",
                CreatedAt = DateTime.UtcNow
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync(cancellationToken);

            string? username = null;
            string? fullName = null;
            if (userId.HasValue)
            {
                var user = await _context.Users.FindAsync(new object[] { userId.Value }, cancellationToken);
                if (user != null)
                {
                    username = user.Username;
                    fullName = user.FullName;
                }
            }

            return new FeedbackResponseDto
            {
                FeedbackId = feedback.FeedbackId,
                UserId = feedback.UserId,
                Username = username,
                FullName = fullName,
                Email = feedback.Email,
                Message = feedback.Message,
                Status = feedback.Status,
                CreatedAt = feedback.CreatedAt
            };
        }

        public async Task<PagedResult<FeedbackResponseDto>> GetAllFeedbacksAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.Feedbacks
                .Include(f => f.User)
                .AsNoTracking()
                .OrderByDescending(f => f.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FeedbackResponseDto
                {
                    FeedbackId = f.FeedbackId,
                    UserId = f.UserId,
                    Username = f.User != null ? f.User.Username : null,
                    FullName = f.User != null ? f.User.FullName : null,
                    Email = f.Email,
                    Message = f.Message,
                    Status = f.Status,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<FeedbackResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task UpdateFeedbackStatusAsync(long feedbackId, string status, CancellationToken cancellationToken = default)
        {
            var feedback = await _context.Feedbacks.FindAsync(new object[] { feedbackId }, cancellationToken);
            if (feedback == null)
            {
                throw new NotFoundException("Không tìm thấy ý kiến đóng góp.");
            }

            feedback.Status = status;
            _context.Feedbacks.Update(feedback);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
