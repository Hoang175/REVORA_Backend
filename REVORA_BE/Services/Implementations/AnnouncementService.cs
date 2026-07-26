using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using REVORA_BE.DTOs;
using REVORA_BE.Models;

namespace REVORA_BE.Services.Implementations
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly AppDbContext _context;

        public AnnouncementService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<AnnouncementResponseDto>> GetActiveAnnouncementsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var announcements = await _context.Announcements
                .Where(a => a.IsActive && a.StartAt <= now && a.EndAt >= now)
                .OrderByDescending(a => a.Priority)
                .ThenByDescending(a => a.CreatedAt)
                .Select(a => new AnnouncementResponseDto
                {
                    AnnouncementId = a.AnnouncementId,
                    Title = a.Title,
                    Description = a.Description,
                    ImageUrl = a.ImageUrl,
                    RedirectUrl = a.RedirectUrl,
                    ButtonText = a.ButtonText,
                    BadgeText = a.BadgeText,
                    Priority = a.Priority,
                    StartAt = a.StartAt,
                    EndAt = a.EndAt,
                    IsActive = a.IsActive,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return announcements;
        }

        public async Task<List<AnnouncementResponseDto>> GetAllAnnouncementsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Announcements
                .OrderByDescending(a => a.Priority)
                .ThenByDescending(a => a.CreatedAt)
                .Select(a => new AnnouncementResponseDto
                {
                    AnnouncementId = a.AnnouncementId,
                    Title = a.Title,
                    Description = a.Description,
                    ImageUrl = a.ImageUrl,
                    RedirectUrl = a.RedirectUrl,
                    ButtonText = a.ButtonText,
                    BadgeText = a.BadgeText,
                    Priority = a.Priority,
                    StartAt = a.StartAt,
                    EndAt = a.EndAt,
                    IsActive = a.IsActive,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<AnnouncementResponseDto> CreateAnnouncementAsync(REVORA_BE.DTOs.Request.AnnouncementCreateDto request, CancellationToken cancellationToken = default)
        {
            var announcement = new Announcement
            {
                Title = request.Title,
                Description = request.Description,
                ImageUrl = request.ImageUrl,
                RedirectUrl = request.RedirectUrl,
                ButtonText = request.ButtonText,
                BadgeText = request.BadgeText,
                Priority = request.Priority,
                StartAt = request.StartAt,
                EndAt = request.EndAt,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync(cancellationToken);

            return new AnnouncementResponseDto
            {
                AnnouncementId = announcement.AnnouncementId,
                Title = announcement.Title,
                Description = announcement.Description,
                ImageUrl = announcement.ImageUrl,
                RedirectUrl = announcement.RedirectUrl,
                ButtonText = announcement.ButtonText,
                BadgeText = announcement.BadgeText,
                Priority = announcement.Priority,
                StartAt = announcement.StartAt,
                EndAt = announcement.EndAt,
                IsActive = announcement.IsActive,
                CreatedAt = announcement.CreatedAt
            };
        }

        public async Task<AnnouncementResponseDto> UpdateAnnouncementAsync(long id, REVORA_BE.DTOs.Request.AnnouncementUpdateDto request, CancellationToken cancellationToken = default)
        {
            var announcement = await _context.Announcements.FindAsync(new object[] { id }, cancellationToken);
            if (announcement == null)
            {
                return null;
            }

            announcement.Title = request.Title;
            announcement.Description = request.Description;
            announcement.ImageUrl = request.ImageUrl;
            announcement.RedirectUrl = request.RedirectUrl;
            announcement.ButtonText = request.ButtonText;
            announcement.BadgeText = request.BadgeText;
            announcement.Priority = request.Priority;
            announcement.StartAt = request.StartAt;
            announcement.EndAt = request.EndAt;
            announcement.IsActive = request.IsActive;

            await _context.SaveChangesAsync(cancellationToken);

            return new AnnouncementResponseDto
            {
                AnnouncementId = announcement.AnnouncementId,
                Title = announcement.Title,
                Description = announcement.Description,
                ImageUrl = announcement.ImageUrl,
                RedirectUrl = announcement.RedirectUrl,
                ButtonText = announcement.ButtonText,
                BadgeText = announcement.BadgeText,
                Priority = announcement.Priority,
                StartAt = announcement.StartAt,
                EndAt = announcement.EndAt,
                IsActive = announcement.IsActive,
                CreatedAt = announcement.CreatedAt
            };
        }

        public async Task<bool> DeleteAnnouncementAsync(long id, CancellationToken cancellationToken = default)
        {
            var announcement = await _context.Announcements.FindAsync(new object[] { id }, cancellationToken);
            if (announcement == null)
            {
                return false;
            }

            _context.Announcements.Remove(announcement);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
