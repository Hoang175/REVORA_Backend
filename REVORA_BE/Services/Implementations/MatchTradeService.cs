using Microsoft.EntityFrameworkCore;
using REVORA_BE.Data;
using REVORA_BE.DTOs.Request;
using REVORA_BE.DTOs.Response;
using REVORA_BE.Helpers;
using REVORA_BE.Models;
using REVORA_BE.Models.Enums;
using REVORA_BE.Repositories.Interfaces;
using REVORA_BE.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using REVORA_BE.Hubs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Implementations
{
    public class MatchTradeService : IMatchTradeService
    {
        private readonly IMatchTradeRepository _repository;
        private readonly IChatRepository _chatRepository;
        private readonly AppDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public MatchTradeService(IMatchTradeRepository repository, IChatRepository chatRepository, AppDbContext context, IHubContext<ChatHub> hubContext, IServiceScopeFactory serviceScopeFactory)
        {
            _repository = repository;
            _chatRepository = chatRepository;
            _context = context;
            _hubContext = hubContext;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<MatchCommunityStatsDto> GetCommunityStatsAsync()
        {
            var activeUsers = await _repository.CountActiveSessionUsersAsync();
            var products = await _repository.GetEligibleProductsForStatsAsync();
            var realProducts = products.Count(p => !p.IsMatchSeed);

            return new MatchCommunityStatsDto
            {
                ActiveParticipants = activeUsers + MatchTradeConstants.DisplayParticipantBoost,
                ProductsWaitingTrade = realProducts + MatchTradeConstants.DisplayProductBoost
            };
        }

        public async Task BroadcastMatchStatsAsync()
        {
            var communityStats = await GetCommunityStatsAsync();
            var filterOptions = await GetFilterOptionsAsync(0);

            var payload = new MatchRealtimeStatsDto
            {
                CommunityStats = communityStats,
                FilterOptions = filterOptions
            };

            await _hubContext.Clients.All.SendAsync("MatchStatsUpdated", payload);
        }

        public async Task<List<MatchOfferingProductDto>> GetMyOfferingProductsAsync(long userId)
        {
            var products = await _repository.GetUserOfferingProductsAsync(userId);
            return products.Select(MapOfferingProduct).ToList();
        }

        public async Task<MatchFilterOptionsDto> GetFilterOptionsAsync(long userId)
        {
            var buckets = new List<MatchFilterBucketDto>();
            foreach (var (min, max, label) in MatchTradeConstants.PriceBuckets)
            {
                var maxFilter = max >= 1_000_000_000 ? decimal.MaxValue : max;
                buckets.Add(new MatchFilterBucketDto
                {
                    Label = label,
                    MinPrice = min,
                    MaxPrice = max >= 1_000_000_000 ? null : max,
                    ProductCount = await _repository.CountEligibleProductsAsync(userId, min, maxFilter, null, new List<long>()),
                    ParticipantCount = await _repository.CountEligibleParticipantsAsync(userId, min, maxFilter, null)
                });
            }

            var cities = await _context.Users
                .Where(u => u.City != null && u.Products.Any(p => p.ProductStatus == "Public" && !p.IsMatchSeed))
                .Select(u => u.City!)
                .Distinct()
                .ToListAsync();

            var cityDtos = new List<MatchCityFilterDto>
            {
                new MatchCityFilterDto
                {
                    City = "Tất cả khu vực",
                    ProductCount = await _repository.CountEligibleProductsAsync(userId, 0, decimal.MaxValue, null, new List<long>()),
                    ParticipantCount = await _repository.CountEligibleParticipantsAsync(userId, 0, decimal.MaxValue, null)
                }
            };
            foreach (var city in cities)
            {
                cityDtos.Add(new MatchCityFilterDto
                {
                    City = city,
                    ProductCount = await _repository.CountEligibleProductsAsync(userId, 0, decimal.MaxValue, city, new List<long>()),
                    ParticipantCount = await _repository.CountEligibleParticipantsAsync(userId, 0, decimal.MaxValue, city)
                });
            }

            return new MatchFilterOptionsDto { PriceBuckets = buckets, Cities = cityDtos };
        }

        public async Task<MatchFilterPreviewDto> PreviewFiltersAsync(long userId, PreviewMatchFiltersRequestDto request)
        {
            var max = request.MaxPrice <= 0 ? decimal.MaxValue : request.MaxPrice;
            var city = string.IsNullOrWhiteSpace(request.City) || request.City == "Tất cả khu vực" || request.City == "All" || request.City.Contains("Tất cả") ? null : request.City.Trim();

            return new MatchFilterPreviewDto
            {
                EstimatedProducts = await _repository.CountEligibleProductsAsync(userId, request.MinPrice, max, city, new List<long>()),
                EstimatedParticipants = await _repository.CountEligibleParticipantsAsync(userId, request.MinPrice, max, city)
            };
        }

        public async Task<MatchSessionResponseDto> StartSessionAsync(long userId, StartMatchSessionRequestDto request)
        {
            if (request.ProductIds == null || request.ProductIds.Count == 0)
                throw new Exception("Chọn ít nhất một sản phẩm để trao đổi.");

            // Use a safe large value that fits in decimal(18,2) instead of decimal.MaxValue
            var max = request.MaxPrice <= 0 ? 999999999999m : request.MaxPrice;

            var owned = await _context.Products
                .Where(p => request.ProductIds.Contains(p.ProductId) && p.SellerId == userId && !p.IsMatchSeed && p.ProductStatus == "Public")
                .Select(p => p.ProductId)
                .ToListAsync();

            if (owned.Count != request.ProductIds.Distinct().Count())
                throw new Exception("Một hoặc nhiều sản phẩm không hợp lệ hoặc không thuộc về bạn.");

            var city = string.IsNullOrWhiteSpace(request.City) || request.City == "Tất cả khu vực" || request.City == "All" || request.City.Contains("Tất cả") ? null : request.City.Trim();

            var session = new MatchSession
            {
                UserId = userId,
                Status = MatchSessionStatus.Active.ToString(),
                MinPrice = request.MinPrice,
                MaxPrice = max,
                City = city,
                StartedAt = DateTime.UtcNow
            };

            session = await _repository.CreateSessionAsync(session, owned);
            session = (await _repository.GetSessionByIdAsync(session.MatchSessionId, userId))!;

            _ = Task.Run(async () => 
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IMatchTradeService>();
                await svc.BroadcastMatchStatsAsync();
            });

            return await MapSessionResponse(session, userId);
        }

        public async Task<MatchSessionResponseDto?> GetActiveSessionAsync(long userId)
        {
            var session = await _repository.GetActiveSessionAsync(userId);
            return session == null ? null : await MapSessionResponse(session, userId);
        }

        public async Task<MatchSwipeResultDto> GetNextCardAsync(long userId, long sessionId)
        {
            var session = await RequireActiveSession(userId, sessionId);
            var swiped = await _repository.GetSwipedProductIdsAsync(sessionId);
            var product = await _repository.GetNextSwipeProductAsync(userId, sessionId, session.MinPrice, session.MaxPrice, session.City, swiped);

            string? badgeName = null;
            if (product?.Seller != null && product.Seller.BadgeId.HasValue)
            {
                var badge = await _context.Badges.AsNoTracking().FirstOrDefaultAsync(b => b.BadgeId == product.Seller.BadgeId.Value);
                badgeName = badge?.Name;
            }

            return new MatchSwipeResultDto
            {
                HasMore = product != null,
                NextProduct = product == null ? null : MapSwipeCard(product, badgeName)
            };
        }

        public async Task<MatchSwipeResultDto> SwipeAsync(long userId, long sessionId, MatchSwipeRequestDto request)
        {
            var session = await RequireActiveSession(userId, sessionId);

            if (!Enum.TryParse<MatchSwipeDirection>(request.Direction, true, out var direction))
                throw new Exception("Direction phải là pass hoặc like.");

            if (await _repository.HasSwipedAsync(sessionId, request.ProductId))
                throw new Exception("Bạn đã vuốt sản phẩm này trong phiên hiện tại.");

            var target = await _context.Products
                .Include(p => p.Seller)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == request.ProductId);

            if (target == null || target.SellerId == userId)
                throw new Exception("Sản phẩm không hợp lệ.");

            await _repository.AddSwipeAsync(new MatchSwipe
            {
                MatchSessionId = sessionId,
                UserId = userId,
                TargetProductId = request.ProductId,
                Direction = direction.ToString(),
                SwipedAt = DateTime.UtcNow
            });

            TradeMatchSummaryDto? newMatch = null;
            var message = direction == MatchSwipeDirection.Pass ? "Đã bỏ qua sản phẩm." : "Đã thêm vào danh sách muốn trao đổi.";

            if (direction == MatchSwipeDirection.Like && !target.IsMatchSeed)
            {
                var offeringProductId = session.OfferingProducts.First().ProductId;

                await _repository.AddInterestNotificationAsync(new MatchInterestNotification
                {
                    OwnerUserId = target.SellerId,
                    InterestedUserId = userId,
                    LikedProductId = target.ProductId,
                    OfferingProductId = offeringProductId,
                    MatchSessionId = sessionId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                if (ChatHub.UserConnections.TryGetValue(target.SellerId, out var targetConnections))
                {
                    await _hubContext.Clients.Clients(targetConnections).SendAsync("InterestNotificationReceived");
                }

                var mutual = await _repository.FindMutualLikeAsync(userId, target.SellerId, sessionId);
                if (mutual != null && mutual.TargetProduct != null && mutual.MatchSession != null)
                {
                    newMatch = await CreateMutualMatchAsync(
                        userId, target.SellerId,
                        sessionId, mutual.MatchSession.MatchSessionId,
                        mutual.TargetProductId, request.ProductId);

                    message = "Chúc mừng! Bạn đã Match thành công!";
                }
            }

            var swiped = await _repository.GetSwipedProductIdsAsync(sessionId);
            var next = await _repository.GetNextSwipeProductAsync(userId, sessionId, session.MinPrice, session.MaxPrice, session.City, swiped);

            string? badgeName = null;
            if (next?.Seller != null && next.Seller.BadgeId.HasValue)
            {
                var badge = await _context.Badges.AsNoTracking().FirstOrDefaultAsync(b => b.BadgeId == next.Seller.BadgeId.Value);
                badgeName = badge?.Name;
            }

            return new MatchSwipeResultDto
            {
                HasMore = next != null,
                NextProduct = next == null ? null : MapSwipeCard(next, badgeName),
                IsMutualMatch = newMatch != null,
                NewMatch = newMatch,
                Message = message
            };
        }

        public async Task UnlikeProductAsync(long userId, long sessionId, long targetProductId)
        {
            var swipe = await _repository.GetMatchSwipeAsync(userId, sessionId, targetProductId);
            if (swipe == null || swipe.TargetProduct == null)
            {
                throw new Exception("Không tìm thấy sản phẩm trong danh sách đã thích.");
            }

            var partnerId = swipe.TargetProduct.SellerId;
            bool hasActiveMatch = await _repository.HasActiveMatchWithUserAsync(userId, partnerId);
            
            if (hasActiveMatch)
            {
                throw new Exception("Sản phẩm này đã nằm trong một Match đang hoạt động. Vui lòng hủy Match trước.");
            }

            await _repository.RemoveSwipeAsync(swipe);
            var removedNotificationIds = await _repository.RemoveInterestNotificationAsync(userId, targetProductId);

            if (ChatHub.UserConnections.TryGetValue(partnerId, out var targetConnections))
            {
                await _hubContext.Clients.Clients(targetConnections).SendAsync("InterestNotificationRemoved", new 
                { 
                    LikedProductId = targetProductId, 
                    InterestedUserId = userId,
                    RemovedNotificationIds = removedNotificationIds 
                });
            }
        }

        public async Task<List<MatchOfferingProductDto>> GetTargetOfferingProductsAsync(long targetUserId)
        {
            var session = await _repository.GetActiveSessionAsync(targetUserId);
            if (session == null)
            {
                var products = await _context.Products
                    .Include(p => p.ProductImages)
                    .Where(p => p.SellerId == targetUserId && p.ProductStatus == "Public" && !p.IsMatchSeed)
                    .ToListAsync();
                return products.Select(MapOfferingProduct).ToList();
            }
            return session.OfferingProducts.Where(op => op.Product != null).Select(op => MapOfferingProduct(op.Product!)).ToList();
        }

        public async Task<MatchSwipeResultDto> BulkSwipeAsync(long userId, MatchBulkSwipeRequestDto request)
        {
            var mySession = await _repository.GetActiveSessionAsync(userId);
            if (mySession == null)
                throw new Exception("Bạn không có phiên Match nào đang hoạt động.");

            TradeMatchSummaryDto? newMatch = null;
            var message = "Đã bỏ qua các sản phẩm.";

            if (request.ProductIds.Any())
            {
                message = "Đã xác nhận lựa chọn của bạn.";
                var myOfferingProductId = mySession.OfferingProducts.FirstOrDefault()?.ProductId;

                foreach (var productId in request.ProductIds)
                {
                    if (await _repository.HasSwipedAsync(mySession.MatchSessionId, productId))
                        continue;

                    var targetProduct = await _context.Products.FindAsync(productId);
                    if (targetProduct == null || targetProduct.SellerId != request.TargetUserId) continue;

                    await _repository.AddSwipeAsync(new MatchSwipe
                    {
                        MatchSessionId = mySession.MatchSessionId,
                        UserId = userId,
                        TargetProductId = productId,
                        Direction = MatchSwipeDirection.Like.ToString(),
                        SwipedAt = DateTime.UtcNow
                    });

                    if (myOfferingProductId.HasValue)
                    {
                        await _repository.AddInterestNotificationAsync(new MatchInterestNotification
                        {
                            OwnerUserId = request.TargetUserId,
                            InterestedUserId = userId,
                            LikedProductId = productId,
                            OfferingProductId = myOfferingProductId.Value,
                            MatchSessionId = mySession.MatchSessionId,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                if (ChatHub.UserConnections.TryGetValue(request.TargetUserId, out var targetConnections))
                {
                    await _hubContext.Clients.Clients(targetConnections).SendAsync("InterestNotificationReceived");
                }

                var mutual = await _repository.FindMutualLikeAsync(userId, request.TargetUserId, mySession.MatchSessionId);
                if (mutual != null && mutual.TargetProduct != null && mutual.MatchSession != null)
                {
                    newMatch = await CreateMutualMatchAsync(
                        userId, request.TargetUserId,
                        mySession.MatchSessionId, mutual.MatchSession.MatchSessionId,
                        mutual.TargetProductId, request.ProductIds.First());
                    message = "Chúc mừng! Bạn đã Match thành công!";
                }
            }

            return new MatchSwipeResultDto
            {
                HasMore = true,
                NextProduct = null,
                IsMutualMatch = newMatch != null,
                NewMatch = newMatch,
                Message = message
            };
        }

        public async Task<TradeConfirmResultDto> NegotiateAsync(long userId, long tradeMatchId, MatchNegotiateRequestDto request)
        {
            var match = await _repository.GetTradeMatchAsync(tradeMatchId, userId)
                ?? throw new Exception("Không tìm thấy Match.");

            if (match.Status != TradeMatchStatus.Active.ToString())
                throw new Exception("Match không còn hoạt động hoặc đã kết thúc.");

            var isLow = match.UserLowId == userId;
            if (isLow)
            {
                match.LowUserNegotiateConfirmed = true;
                match.LowUserSelectedProductIds = string.Join(",", request.SelectedProductIds);
            }
            else
            {
                match.HighUserNegotiateConfirmed = true;
                match.HighUserSelectedProductIds = string.Join(",", request.SelectedProductIds);
            }

            await _repository.UpdateTradeMatchAsync(match);

            if (match.LowUserNegotiateConfirmed && match.HighUserNegotiateConfirmed)
            {
                var conv = await _chatRepository.GetConversationAsync(match.UserLowId, match.UserHighId)
                    ?? await _chatRepository.CreateConversationAsync(match.UserLowId, match.UserHighId);

                match.ConversationId = conv.ConversationId;
                await _repository.UpdateTradeMatchAsync(match);

                var now = DateTime.UtcNow;
                await _chatRepository.AddMessageAsync(new Message
                {
                    ConversationId = conv.ConversationId,
                    SenderId = match.UserLowId,
                    Content = "🎉 Hai bạn đã xác nhận đưa sản phẩm vào thương lượng! Hãy bắt đầu trao đổi chi tiết.",
                    ProductRefId = request.SelectedProductIds.FirstOrDefault(),
                    Source = "MATCH_TRADE",
                    CreatedAt = now
                });
                await _chatRepository.UpdateConversationLastMessageAtAsync(conv.ConversationId, now);

                if (ChatHub.UserConnections.TryGetValue(match.UserLowId, out var lowConns))
                    await _hubContext.Clients.Clients(lowConns).SendAsync("ChatCreated", new { TradeMatchId = tradeMatchId, ConversationId = conv.ConversationId });
                
                if (ChatHub.UserConnections.TryGetValue(match.UserHighId, out var highConns))
                    await _hubContext.Clients.Clients(highConns).SendAsync("ChatCreated", new { TradeMatchId = tradeMatchId, ConversationId = conv.ConversationId });

                return BuildConfirmResult(match, userId, false, "Cả hai đã xác nhận. Phòng chat đã được tạo!");
            }

            var partnerId = isLow ? match.UserHighId : match.UserLowId;
            if (ChatHub.UserConnections.TryGetValue(partnerId, out var partnerConns))
            {
                await _hubContext.Clients.Clients(partnerConns).SendAsync("PartnerNegotiateConfirmed", new { TradeMatchId = tradeMatchId, SelectedProductIds = request.SelectedProductIds });
            }

            return BuildConfirmResult(match, userId, false, "Đang chờ đối phương xác nhận...");
        }

        public async Task EndSessionAsync(long userId, long sessionId)
        {
            var session = await _repository.GetSessionByIdAsync(sessionId, userId)
                ?? throw new Exception("Không tìm thấy phiên Match.");

            if (session.Status != MatchSessionStatus.Active.ToString())
                return;

            var productIds = session.OfferingProducts.Where(op => op.Product != null).Select(op => op.ProductId).ToList();

            await _repository.CleanupSessionTempDataAsync(sessionId, userId);
            await _repository.EndSessionAsync(sessionId);

            _ = Task.Run(async () => 
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IMatchTradeService>();
                await svc.BroadcastMatchStatsAsync();
            });

            if (productIds.Any())
            {
                await _hubContext.Clients.All.SendAsync("ProductsRemoved", productIds);
            }
        }

        public async Task EndActiveSessionAsync(long userId)
        {
            var activeSession = await _repository.GetActiveSessionAsync(userId);
            if (activeSession != null)
            {
                await EndSessionAsync(userId, activeSession.MatchSessionId);
            }
        }

        public async Task ExpireSessionAsync(long sessionId)
        {
            var session = await _context.MatchSessions
                .Include(s => s.OfferingProducts).ThenInclude(op => op.Product)
                .FirstOrDefaultAsync(s => s.MatchSessionId == sessionId);

            if (session == null || session.Status != MatchSessionStatus.Active.ToString())
                return;

            var productIds = session.OfferingProducts.Where(op => op.Product != null).Select(op => op.ProductId).ToList();

            await _repository.CleanupSessionTempDataAsync(sessionId, session.UserId);
            await _repository.EndSessionAsync(sessionId, MatchSessionStatus.Expired.ToString());

            _ = Task.Run(async () => 
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IMatchTradeService>();
                await svc.BroadcastMatchStatsAsync();
            });

            if (productIds.Any())
            {
                await _hubContext.Clients.All.SendAsync("ProductsRemoved", productIds);
            }
        }

        public async Task<List<TradeMatchSummaryDto>> GetMyMatchesAsync(long userId, string? status = null)
        {
            var matches = await _repository.GetUserTradeMatchesAsync(userId, status);
            var dtos = new List<TradeMatchSummaryDto>();
            foreach (var m in matches)
            {
                dtos.Add(await MapTradeMatchWithProductsAsync(m, userId));
            }
            return dtos;
        }

        public async Task<TradeMatchSummaryDto?> GetMatchDetailAsync(long userId, long tradeMatchId)
        {
            var match = await _repository.GetTradeMatchAsync(tradeMatchId, userId);
            return match == null ? null : await MapTradeMatchWithProductsAsync(match, userId);
        }

        public async Task<TradeConfirmResultDto> ConfirmTradeAsync(long userId, long tradeMatchId)
        {
            var match = await _repository.GetTradeMatchAsync(tradeMatchId, userId)
                ?? throw new Exception("Không tìm thấy Match.");

            if (match.Status != TradeMatchStatus.Active.ToString())
                throw new Exception("Match không còn hoạt động.");

            var isLow = match.UserLowId == userId;
            if (isLow) match.LowUserConfirmed = true;
            else match.HighUserConfirmed = true;

            var msg = isLow
                ? (match.HighUserConfirmed ? "Cả hai đã đồng ý trao đổi!" : "Bạn đã đồng ý trao đổi.")
                : (match.LowUserConfirmed ? "Cả hai đã đồng ý trao đổi!" : "Bạn đã đồng ý trao đổi.");

            if (!match.LowUserConfirmed || !match.HighUserConfirmed)
            {
                if (!isLow && match.LowUserConfirmed)
                    msg = "Đối phương đã đồng ý trao đổi.";
                else if (isLow && match.HighUserConfirmed)
                    msg = "Đối phương đã đồng ý trao đổi.";

                await _repository.UpdateTradeMatchAsync(match);

                var partnerId = isLow ? match.UserHighId : match.UserLowId;
                if (ChatHub.UserConnections.TryGetValue(partnerId, out var partnerConns))
                {
                    await _hubContext.Clients.Clients(partnerConns).SendAsync("TradeConfirmRequested", new { TradeMatchId = tradeMatchId });
                }

                return BuildConfirmResult(match, userId, false, msg);
            }

            match.Status = TradeMatchStatus.Completed.ToString();
            match.CompletedAt = DateTime.UtcNow;

            await _repository.UpdateTradeMatchAsync(match);
            await _repository.IncrementTradeSuccessAsync(match.UserLowId);
            await _repository.IncrementTradeSuccessAsync(match.UserHighId);

            await _repository.CleanupSessionTempDataAsync(match.SessionLowUserId, match.UserLowId, match.UserHighId);
            await _repository.CleanupSessionTempDataAsync(match.SessionHighUserId, match.UserHighId, match.UserLowId);

            var lowSession = await _context.MatchSessions.FindAsync(match.SessionLowUserId);
            var highSession = await _context.MatchSessions.FindAsync(match.SessionHighUserId);
            if (lowSession != null) { lowSession.Status = MatchSessionStatus.Matched.ToString(); lowSession.EndedAt = DateTime.UtcNow; }
            if (highSession != null) { highSession.Status = MatchSessionStatus.Matched.ToString(); highSession.EndedAt = DateTime.UtcNow; }
            await _context.SaveChangesAsync();

            if (ChatHub.UserConnections.TryGetValue(match.UserLowId, out var lowConns)) 
                await _hubContext.Clients.Clients(lowConns).SendAsync("TradeCompleted", new { TradeMatchId = tradeMatchId });
            if (ChatHub.UserConnections.TryGetValue(match.UserHighId, out var highConns)) 
                await _hubContext.Clients.Clients(highConns).SendAsync("TradeCompleted", new { TradeMatchId = tradeMatchId });

            return BuildConfirmResult(match, userId, true, "Trao đổi thành công! Đã ghi nhận Trade Success.");
        }

        public async Task<TradeConfirmResultDto> DeclineConfirmAsync(long userId, long tradeMatchId)
        {
            var match = await _repository.GetTradeMatchAsync(tradeMatchId, userId)
                ?? throw new Exception("Không tìm thấy Match.");

            if (match.Status != TradeMatchStatus.Active.ToString())
                throw new Exception("Match không còn hoạt động.");

            var isLow = match.UserLowId == userId;
            if (isLow) match.HighUserConfirmed = false;
            else match.LowUserConfirmed = false;

            await _repository.UpdateTradeMatchAsync(match);

            var partnerId = isLow ? match.UserHighId : match.UserLowId;
            if (ChatHub.UserConnections.TryGetValue(partnerId, out var partnerConns))
            {
                await _hubContext.Clients.Clients(partnerConns).SendAsync("TradeConfirmDeclined", new { TradeMatchId = tradeMatchId });
            }

            return BuildConfirmResult(match, userId, false, "Đã từ chối xác nhận trao đổi.");
        }

        public async Task<TradeConfirmResultDto> CancelMatchAsync(long userId, long tradeMatchId, bool isExpired)
        {
            var match = await _repository.GetTradeMatchAsync(tradeMatchId, userId)
                ?? throw new Exception("Không tìm thấy Match.");

            if (match.Status != TradeMatchStatus.Active.ToString())
                throw new Exception("Match không còn hoạt động.");

            // 1. Xóa TradeMatch
            _context.TradeMatches.Remove(match);

            var partnerId = match.UserLowId == userId ? match.UserHighId : match.UserLowId;

            // 2. Xóa Swipes giữa 2 user
            var swipes = await _context.MatchSwipes
                .Include(s => s.TargetProduct)
                .Where(s => 
                    (s.MatchSessionId == match.SessionLowUserId && s.TargetProduct != null && s.TargetProduct.SellerId == match.UserHighId) ||
                    (s.MatchSessionId == match.SessionHighUserId && s.TargetProduct != null && s.TargetProduct.SellerId == match.UserLowId))
                .ToListAsync();
            _context.MatchSwipes.RemoveRange(swipes);

            // 3. Xóa Interest Notifications giữa 2 user
            var notifications = await _context.MatchInterestNotifications
                .Where(n => 
                    (n.InterestedUserId == match.UserLowId && n.OwnerUserId == match.UserHighId) ||
                    (n.InterestedUserId == match.UserHighId && n.OwnerUserId == match.UserLowId))
                .ToListAsync();
            _context.MatchInterestNotifications.RemoveRange(notifications);

            var lowSession = await _context.MatchSessions.FindAsync(match.SessionLowUserId);
            var highSession = await _context.MatchSessions.FindAsync(match.SessionHighUserId);
            if (lowSession != null) 
            { 
                lowSession.Status = MatchSessionStatus.Active.ToString(); 
                lowSession.EndedAt = null; 
            }
            if (highSession != null) 
            { 
                highSession.Status = MatchSessionStatus.Active.ToString(); 
                highSession.EndedAt = null; 
            }

            await _context.SaveChangesAsync();

            // 4. Báo SignalR cho đối phương (truyền isExpired)
            if (ChatHub.UserConnections.TryGetValue(partnerId, out var partnerConns))
            {
                await _hubContext.Clients.Clients(partnerConns).SendAsync("MatchCancelled", new { TradeMatchId = tradeMatchId, IsExpired = isExpired });
            }

            return BuildConfirmResult(match, userId, true, isExpired ? "Match đã hết hạn." : "Đã hủy Match.");
        }

        public async Task<TradeConfirmResultDto> LeaveTradeAsync(long userId, long tradeMatchId)
        {
            var match = await _repository.GetTradeMatchAsync(tradeMatchId, userId)
                ?? throw new Exception("Không tìm thấy Match.");

            if (match.Status != TradeMatchStatus.Active.ToString() && match.Status != TradeMatchStatus.Completed.ToString())
                throw new Exception("Match đã được đóng.");

            // 1. Delete Conversation (cascade deletes Messages)
            if (match.ConversationId.HasValue)
            {
                var conversation = await _context.Conversations.FindAsync(match.ConversationId.Value);
                if (conversation != null)
                {
                    _context.Conversations.Remove(conversation);
                }
            }

            // 2. Delete the TradeMatch itself
            _context.TradeMatches.Remove(match);

            var partnerId = match.UserLowId == userId ? match.UserHighId : match.UserLowId;

            // 3. Delete ALL swipes for both sessions (not just mutual ones)
            var allSwipesToDelete = await _context.MatchSwipes
                .Where(s => s.MatchSessionId == match.SessionLowUserId || s.MatchSessionId == match.SessionHighUserId)
                .ToListAsync();
            _context.MatchSwipes.RemoveRange(allSwipesToDelete);

            // 4. Delete ALL interest notifications for both sessions/users
            var allNotifications = await _context.MatchInterestNotifications
                .Where(n => n.MatchSessionId == match.SessionLowUserId
                    || n.MatchSessionId == match.SessionHighUserId
                    || n.InterestedUserId == match.UserLowId
                    || n.OwnerUserId == match.UserLowId
                    || n.InterestedUserId == match.UserHighId
                    || n.OwnerUserId == match.UserHighId)
                .ToListAsync();
            _context.MatchInterestNotifications.RemoveRange(allNotifications);

            // 5. Delete ALL other active TradeMatches involving either user (pending matches)
            var otherTradeMatches = await _context.TradeMatches
                .Where(t => t.TradeMatchId != tradeMatchId
                    && t.Status == TradeMatchStatus.Active.ToString()
                    && (t.UserLowId == match.UserLowId || t.UserHighId == match.UserLowId
                        || t.UserLowId == match.UserHighId || t.UserHighId == match.UserHighId))
                .ToListAsync();
            foreach (var otherMatch in otherTradeMatches)
            {
                if (otherMatch.ConversationId.HasValue)
                {
                    var conv = await _context.Conversations.FindAsync(otherMatch.ConversationId.Value);
                    if (conv != null) _context.Conversations.Remove(conv);
                }
                _context.TradeMatches.Remove(otherMatch);
            }

            // 6. Delete offering products for both sessions
            var offeringProducts = await _context.MatchSessionProducts
                .Where(op => op.MatchSessionId == match.SessionLowUserId || op.MatchSessionId == match.SessionHighUserId)
                .ToListAsync();
            _context.MatchSessionProducts.RemoveRange(offeringProducts);

            // 7. Delete both MatchSessions completely
            var lowSession = await _context.MatchSessions.FindAsync(match.SessionLowUserId);
            var highSession = await _context.MatchSessions.FindAsync(match.SessionHighUserId);
            if (lowSession != null) _context.MatchSessions.Remove(lowSession);
            if (highSession != null) _context.MatchSessions.Remove(highSession);

            await _context.SaveChangesAsync();

            // Notify partner that the entire session has been terminated
            if (ChatHub.UserConnections.TryGetValue(partnerId, out var partnerConns))
            {
                await _hubContext.Clients.Clients(partnerConns).SendAsync("TradeCancelled", new { TradeMatchId = tradeMatchId });
                await _hubContext.Clients.Clients(partnerConns).SendAsync("SessionTerminated", new { Reason = "Đối phương đã kết thúc phiên Match." });
            }

            // Broadcast updated stats
            _ = Task.Run(async () =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IMatchTradeService>();
                await svc.BroadcastMatchStatsAsync();
            });

            return new TradeConfirmResultDto
            {
                TradeMatchId = match.TradeMatchId,
                Status = "Ended",
                Message = "Đã kết thúc toàn bộ phiên Match."
            };
        }

        public async Task<TradeConfirmResultDto> FinishTradeAsync(long userId, long tradeMatchId)
        {
            var match = await _repository.GetTradeMatchAsync(tradeMatchId, userId)
                ?? throw new Exception("Không tìm thấy Match.");

            if (match.Status != TradeMatchStatus.Completed.ToString())
                throw new Exception("Chỉ có thể hoàn tất khi Match đã thành công.");

            // Giữ lại lịch sử trò chuyện (Conversation) theo yêu cầu
            // if (match.ConversationId.HasValue)
            // {
            //     var conversation = await _context.Conversations.FindAsync(match.ConversationId.Value);
            //     if (conversation != null)
            //     {
            //         _context.Conversations.Remove(conversation);
            //     }
            //     match.ConversationId = null;
            // }

            // 2. Delete User A's Match Session and Swipe Session
            var userSessionId = match.UserLowId == userId ? match.SessionLowUserId : match.SessionHighUserId;
            
            // Cleanup temp data first (Swipes, Notifications)
            await _repository.CleanupSessionTempDataAsync(userSessionId, userId);

            // Hard delete the MatchSession itself
            var session = await _context.MatchSessions.FindAsync(userSessionId);
            if (session != null)
            {
                _context.MatchSessions.Remove(session);
            }

            await _context.SaveChangesAsync();

            // No SignalR notification to partner - let them stay in UI without knowing
            // Broadcast community stats update since a session was deleted
            _ = Task.Run(async () => 
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IMatchTradeService>();
                await svc.BroadcastMatchStatsAsync();
            });

            return new TradeConfirmResultDto
            {
                TradeMatchId = match.TradeMatchId,
                Status = "Finished",
                Message = "Đã hoàn tất phiên làm việc."
            };
        }

        private async Task<TradeMatchSummaryDto> CreateMutualMatchAsync(
            long userAId, long userBId,
            long sessionAId, long sessionBId,
            long productAId, long productBId)
        {
            var (low, high) = userAId < userBId ? (userAId, userBId) : (userBId, userAId);
            var productLow = userAId < userBId ? productAId : productBId;
            var productHigh = userAId < userBId ? productBId : productAId;
            var sessionLow = userAId < userBId ? sessionAId : sessionBId;
            var sessionHigh = userAId < userBId ? sessionBId : sessionAId;

            var existing = await _context.TradeMatches.FirstOrDefaultAsync(t =>
                t.UserLowId == low && t.UserHighId == high && t.Status == TradeMatchStatus.Active.ToString());
            if (existing != null)
                return await MapTradeMatchWithProductsAsync(existing, userAId);

            var lowSwipes = await _context.MatchSwipes
                .Include(s => s.TargetProduct)
                .Where(s => s.UserId == low && s.MatchSessionId == sessionLow && s.Direction == "Like" && s.TargetProduct != null && s.TargetProduct.SellerId == high)
                .Select(s => s.TargetProductId)
                .ToListAsync();

            var highSwipes = await _context.MatchSwipes
                .Include(s => s.TargetProduct)
                .Where(s => s.UserId == high && s.MatchSessionId == sessionHigh && s.Direction == "Like" && s.TargetProduct != null && s.TargetProduct.SellerId == low)
                .Select(s => s.TargetProductId)
                .ToListAsync();

            var match = await _repository.CreateTradeMatchAsync(new TradeMatch
            {
                UserLowId = low,
                UserHighId = high,
                ProductLowUserId = productLow,
                ProductHighUserId = productHigh,
                SessionLowUserId = sessionLow,
                SessionHighUserId = sessionHigh,
                LowUserSelectedProductIds = string.Join(",", lowSwipes),
                HighUserSelectedProductIds = string.Join(",", highSwipes),
                ConversationId = null,
                Status = TradeMatchStatus.Active.ToString(),
                CreatedAt = DateTime.UtcNow
            });

            var now = DateTime.UtcNow;

            var mutualNotifs = await _context.MatchInterestNotifications
                .Where(n => 
                    (n.InterestedUserId == low && n.OwnerUserId == high) ||
                    (n.InterestedUserId == high && n.OwnerUserId == low))
                .ToListAsync();
            _context.MatchInterestNotifications.RemoveRange(mutualNotifs);

            var sessionA = await _context.MatchSessions.FindAsync(sessionAId);
            var sessionB = await _context.MatchSessions.FindAsync(sessionBId);
            if (sessionA != null) { sessionA.Status = MatchSessionStatus.Matched.ToString(); sessionA.EndedAt = now; }
            if (sessionB != null) { sessionB.Status = MatchSessionStatus.Matched.ToString(); sessionB.EndedAt = now; }
            await _context.SaveChangesAsync();

            match = (await _repository.GetTradeMatchAsync(match.TradeMatchId, userAId))!;
            var summary = await MapTradeMatchWithProductsAsync(match, userAId);

            if (ChatHub.UserConnections.TryGetValue(userBId, out var connectionIds))
            {
                var summaryForB = await MapTradeMatchWithProductsAsync(match, userBId);
                await _hubContext.Clients.Clients(connectionIds.ToList()).SendAsync("MutualMatchCreated", summaryForB);
            }

            return summary;
        }

        private async Task<MatchSession> RequireActiveSession(long userId, long sessionId)
        {
            var session = await _repository.GetSessionByIdAsync(sessionId, userId)
                ?? throw new Exception("Không tìm thấy phiên Match.");

            if (session.Status != MatchSessionStatus.Active.ToString())
                throw new Exception("Phiên Match đã kết thúc.");

            return session;
        }

        private async Task<MatchSessionResponseDto> MapSessionResponse(MatchSession session, long userId)
        {
            var max = session.MaxPrice <= 0 ? decimal.MaxValue : session.MaxPrice;
            return new MatchSessionResponseDto
            {
                MatchSessionId = session.MatchSessionId,
                Status = session.Status,
                MinPrice = session.MinPrice,
                MaxPrice = session.MaxPrice,
                City = session.City,
                OfferingProducts = session.OfferingProducts
                    .Where(op => op.Product != null)
                    .Select(op => MapOfferingProduct(op.Product!))
                    .ToList(),
                EstimatedProducts = await _repository.CountEligibleProductsAsync(userId, session.MinPrice, max, session.City, new List<long>()),
                EstimatedParticipants = await _repository.CountEligibleParticipantsAsync(userId, session.MinPrice, max, session.City),
                StartedAt = session.StartedAt
            };
        }

        private static MatchOfferingProductDto MapOfferingProduct(Product p) => new()
        {
            ProductId = p.ProductId,
            Title = p.Title,
            Price = p.Price,
            ImageUrl = p.ProductImages.FirstOrDefault()?.ImageUrl,
            ProductStatus = p.ProductStatus
        };

        private static MatchSwipeCardDto MapSwipeCard(Product p, string? badgeName) => new()
        {
            ProductId = p.ProductId,
            Title = p.Title,
            Price = p.Price,
            Condition = p.Condition,
            Brand = p.Brand,
            ImageUrl = p.ProductImages.FirstOrDefault()?.ImageUrl,
            SellerId = p.SellerId,
            SellerName = p.Seller?.FullName ?? "Người bán",
            SellerCity = p.Seller?.City,
            SellerAvatar = p.Seller?.AvatarUrl,
            IsPremium = p.HighlightStatus && p.HighlightExpiredAt > DateTime.UtcNow,
            HasBadge = p.Seller?.BadgeId != null,
            SellerBadgeName = badgeName,
            IsMatchSeed = p.IsMatchSeed
        };

        private async Task<TradeMatchSummaryDto> MapTradeMatchWithProductsAsync(TradeMatch m, long currentUserId)
        {
            var isLow = m.UserLowId == currentUserId;
            var partner = isLow ? m.UserHigh : m.UserLow;

            var lowSession = await _context.MatchSessions.Include(s => s.OfferingProducts).ThenInclude(op => op.Product).ThenInclude(p => p!.ProductImages).FirstOrDefaultAsync(s => s.MatchSessionId == m.SessionLowUserId);
            var highSession = await _context.MatchSessions.Include(s => s.OfferingProducts).ThenInclude(op => op.Product).ThenInclude(p => p!.ProductImages).FirstOrDefaultAsync(s => s.MatchSessionId == m.SessionHighUserId);

            var mySession = isLow ? lowSession : highSession;
            var partnerSession = isLow ? highSession : lowSession;

            string? partnerBadgeName = null;
            if (partner != null && partner.BadgeId.HasValue)
            {
                var badge = await _context.Badges.AsNoTracking().FirstOrDefaultAsync(b => b.BadgeId == partner.BadgeId.Value);
                partnerBadgeName = badge?.Name;
            }

            return new TradeMatchSummaryDto
            {
                TradeMatchId = m.TradeMatchId,
                ConversationId = m.ConversationId ?? 0,
                PartnerUserId = isLow ? m.UserHighId : m.UserLowId,
                PartnerName = partner?.FullName ?? "",
                PartnerAvatar = partner?.AvatarUrl,
                PartnerBadgeName = partnerBadgeName,
                MyProducts = mySession?.OfferingProducts.Where(op => op.Product != null).Select(op => MapOfferingProduct(op.Product!)).ToList() ?? new List<MatchOfferingProductDto>(),
                PartnerProducts = partnerSession?.OfferingProducts.Where(op => op.Product != null).Select(op => MapOfferingProduct(op.Product!)).ToList() ?? new List<MatchOfferingProductDto>(),
                Status = m.Status,
                MyConfirmed = isLow ? m.LowUserConfirmed : m.HighUserConfirmed,
                PartnerConfirmed = isLow ? m.HighUserConfirmed : m.LowUserConfirmed,
                MyNegotiateConfirmed = isLow ? m.LowUserNegotiateConfirmed : m.HighUserNegotiateConfirmed,
                PartnerNegotiateConfirmed = isLow ? m.HighUserNegotiateConfirmed : m.LowUserNegotiateConfirmed,
                MySelectedProductIds = (isLow ? m.LowUserSelectedProductIds : m.HighUserSelectedProductIds)?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(long.Parse).ToList() ?? new List<long>(),
                PartnerSelectedProductIds = (isLow ? m.HighUserSelectedProductIds : m.LowUserSelectedProductIds)?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(long.Parse).ToList() ?? new List<long>(),
                CreatedAt = m.CreatedAt
            };
        }

        private static TradeConfirmResultDto BuildConfirmResult(TradeMatch m, long userId, bool completed, string message)
        {
            var isLow = m.UserLowId == userId;
            return new TradeConfirmResultDto
            {
                TradeMatchId = m.TradeMatchId,
                Status = m.Status,
                MyConfirmed = isLow ? m.LowUserConfirmed : m.HighUserConfirmed,
                PartnerConfirmed = isLow ? m.HighUserConfirmed : m.LowUserConfirmed,
                IsCompleted = completed,
                Message = message
            };
        }

        public async Task<List<MatchLikedProductDto>> GetMyLikedProductsAsync(long userId, long sessionId)
        {
            var swipes = await _context.MatchSwipes
                .Where(s => s.MatchSessionId == sessionId && s.UserId == userId && s.Direction == "Like")
                .Include(s => s.TargetProduct)
                    .ThenInclude(p => p!.ProductImages)
                .Include(s => s.TargetProduct)
                    .ThenInclude(p => p!.Seller)
                .OrderByDescending(s => s.SwipedAt)
                .ToListAsync();

            return swipes
                .Where(s => s.TargetProduct != null)
                .Select(s => new MatchLikedProductDto
                {
                    ProductId = s.TargetProduct!.ProductId,
                    Title = s.TargetProduct.Title,
                    Price = s.TargetProduct.Price,
                    ImageUrl = s.TargetProduct.ProductImages.FirstOrDefault()?.ImageUrl,
                    SellerName = s.TargetProduct.Seller?.FullName ?? "Người bán",
                    SwipedAt = s.SwipedAt
                }).ToList();
        }

        public async Task<List<MatchInterestInboxItemDto>> GetInterestInboxAsync(long userId)
        {
            var notifications = await _context.MatchInterestNotifications
                .Where(n => n.OwnerUserId == userId)
                .Include(n => n.InterestedUser)
                .Include(n => n.LikedProduct)
                    .ThenInclude(p => p!.ProductImages)
                .Include(n => n.OfferingProduct)
                    .ThenInclude(p => p!.ProductImages)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            var badgeMap = await _context.Badges.AsNoTracking().ToDictionaryAsync(b => b.BadgeId, b => b.Name);

            return notifications.Select(n =>
            {
                string? badgeName = null;
                if (n.InterestedUser != null && n.InterestedUser.BadgeId.HasValue)
                {
                    badgeMap.TryGetValue(n.InterestedUser.BadgeId.Value, out badgeName);
                }

                return new MatchInterestInboxItemDto
                {
                    NotificationId = n.MatchInterestNotificationId,
                    InterestedUserId = n.InterestedUserId,
                    InterestedUserName = n.InterestedUser?.FullName ?? "Người dùng",
                    InterestedUserAvatar = n.InterestedUser?.AvatarUrl,
                    InterestedUserBadgeName = badgeName,
                    LikedProductId = n.LikedProductId,
                    LikedProductTitle = n.LikedProduct?.Title ?? "",
                    LikedProductImage = n.LikedProduct?.ProductImages.FirstOrDefault()?.ImageUrl,
                    OfferingProductId = n.OfferingProductId,
                    OfferingProductTitle = n.OfferingProduct?.Title ?? "",
                    OfferingProductImage = n.OfferingProduct?.ProductImages.FirstOrDefault()?.ImageUrl,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                };
            }).ToList();
        }
        public async Task<TradeMutualLikesDto> GetMutualLikesInTradeAsync(long userId, long tradeMatchId)
        {
            var match = await _repository.GetTradeMatchAsync(tradeMatchId, userId)
                ?? throw new Exception("Không tìm thấy Match.");

            var partnerId = match.UserLowId == userId ? match.UserHighId : match.UserLowId;

            // My likes (swipes where UserId == userId AND TargetProduct.SellerId == partnerId)
            var mySwipes = await _context.MatchSwipes
                .Include(s => s.TargetProduct).ThenInclude(p => p!.ProductImages)
                .Where(s => s.UserId == userId && s.TargetProduct != null && s.TargetProduct.SellerId == partnerId)
                .ToListAsync();

            // Partner likes (swipes where UserId == partnerId AND TargetProduct.SellerId == userId)
            var partnerSwipes = await _context.MatchSwipes
                .Include(s => s.TargetProduct).ThenInclude(p => p!.ProductImages)
                .Where(s => s.UserId == partnerId && s.TargetProduct != null && s.TargetProduct.SellerId == userId)
                .ToListAsync();

            return new TradeMutualLikesDto
            {
                MyLikedProducts = mySwipes.Where(s => s.TargetProduct != null).Select(s => MapOfferingProduct(s.TargetProduct!)).ToList(),
                PartnerLikedProducts = partnerSwipes.Where(s => s.TargetProduct != null).Select(s => MapOfferingProduct(s.TargetProduct!)).ToList()
            };
        }
    }
}
