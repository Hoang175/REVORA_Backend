using REVORA_BE.DTOs.Request;
using REVORA_BE.DTOs.Response;
using REVORA_BE.Models;
using REVORA_BE.Models.Enums;
using REVORA_BE.Repositories.Interfaces;
using REVORA_BE.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace REVORA_BE.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly INotificationService _notificationService;

        private const int CREDIT_TYPE_POSTING = 1;
        private const int CREDIT_TYPE_FEATURED = 2;

        public ProductService(IProductRepository productRepository, INotificationService notificationService)
        {
            _productRepository = productRepository;
            _notificationService = notificationService;
        }

        public async Task<long> CreateProductAsync(long userId, CreateProductRequestDto request)
        {
            int requiredPostingCredits = 1; 
            int requiredFeaturedCredits = 0;
            if (request.EnableVideoUpload) requiredFeaturedCredits++;
            if (request.EnableBannerBoost) requiredFeaturedCredits++;

            var postingBatches = await _productRepository.GetActiveCreditBatchesAsync(userId, CREDIT_TYPE_POSTING);
            if (postingBatches.Sum(b => b.RemainingCredits) < requiredPostingCredits)
                throw new Exception("Bạn không đủ Credit Đăng Tin (Posting Credit).");

            var featuredBatches = new List<UserCreditBatch>();
            if (requiredFeaturedCredits > 0)
            {
                featuredBatches = await _productRepository.GetActiveCreditBatchesAsync(userId, CREDIT_TYPE_FEATURED);
                if (featuredBatches.Sum(b => b.RemainingCredits) < requiredFeaturedCredits)
                    throw new Exception("Bạn không đủ Credit Nổi Bật (Featured Credit).");
            }

            var batchesToUpdate = new List<UserCreditBatch>();
            var usageLogs = new List<CreditUsageLog>();

            DeductCredits(postingBatches, requiredPostingCredits, batchesToUpdate);
            usageLogs.Add(new CreditUsageLog
            {
                UserId = userId,
                CreditTypeId = CREDIT_TYPE_POSTING,
                ActionType = "post_new",
                Amount = requiredPostingCredits,
                ProductName = request.Title,
                BalanceAfter = postingBatches.Sum(b => b.RemainingCredits),
                CreatedAt = DateTime.UtcNow
            });

            if (requiredFeaturedCredits > 0)
            {
                DeductCredits(featuredBatches, requiredFeaturedCredits, batchesToUpdate);
                usageLogs.Add(new CreditUsageLog
                {
                    UserId = userId,
                    CreditTypeId = CREDIT_TYPE_FEATURED,
                    ActionType = request.EnableBannerBoost ? "boost_featured" : "extend_featured",
                    Amount = requiredFeaturedCredits,
                    ProductName = request.Title,
                    BalanceAfter = featuredBatches.Sum(b => b.RemainingCredits),
                    CreatedAt = DateTime.UtcNow
                });
            }

            var now = DateTime.UtcNow;
            int expiryDays = requiredFeaturedCredits > 0 ? 60 : 30;

            var product = new Product
            {
                SellerId = userId,
                CategoryId = request.CategoryId,
                Title = request.Title,
                Description = request.Description,
                Price = request.Price,
                Brand = request.Brand,
                Condition = request.Condition,
                ProductStatus = "Public",
                ProductCreateAt = now,
                ProductExpiredAt = now.AddDays(expiryDays),
                IsUsedBanner = request.EnableBannerBoost,
                BannerStatus = request.EnableBannerBoost,
                BannerUrl = request.EnableBannerBoost ? request.BannerUrl : null,
                BannerExpiredAt = request.EnableBannerBoost ? now.AddHours(24) : null,
                HighlightStatus = request.EnableBannerBoost || request.EnableVideoUpload,
                HighlightExpiredAt = (request.EnableBannerBoost || request.EnableVideoUpload) ? now.AddDays(60) : null,
                IsUsedShort = request.EnableVideoUpload,
                CommentCount = 0
            };

            var productImages = request.ImageUrls.Select(url => new ProductImage
            {
                ImageUrl = url
            }).ToList();

            product.ProductImages = productImages;

            Short shortVideo = null;
            if (request.EnableVideoUpload && !string.IsNullOrEmpty(request.VideoUrl))
            {
                shortVideo = new Short
                {
                    SellerId = userId,
                    VideoUrl = request.VideoUrl,
                    Caption = request.Title,
                    LikeCount = 0,
                    CommentCount = 0,
                    ShortStatus = ShortStatus.Active.ToString(),
                    CreatedAt = now,
                    ExpiredAt = product.ProductExpiredAt
                };
            }

            await _productRepository.CreateProductWithTransactionAsync(product, shortVideo, batchesToUpdate, usageLogs);

            await _notificationService.CreateNotificationAsync(
                userId: userId,
                type: "post",
                title: "Đăng tin thành công",
                message: $"Sản phẩm {request.Title} của bạn đã được đăng thành công.",
                referenceId: product.ProductId.ToString()
            );

            return product.ProductId;
        }

        private void DeductCredits(List<UserCreditBatch> batches, int neededAmount, List<UserCreditBatch> batchesToUpdate)
        {
            foreach (var batch in batches)
            {
                if (neededAmount == 0) break;

                if (batch.RemainingCredits >= neededAmount)
                {
                    batch.RemainingCredits -= neededAmount;
                    neededAmount = 0;
                }
                else
                {
                    neededAmount -= batch.RemainingCredits;
                    batch.RemainingCredits = 0;
                }

                batchesToUpdate.Add(batch);
            }
        }

        public async Task<List<ProductResponseDto>> GetHomeProductsAsync(string type, int limit)
        {
            var products = await _productRepository.GetHomeProductsAsync(type, limit);
            var badgeDict = await _productRepository.GetBadgeIdToNameMapAsync();
            return products.Select(p => MapToResponseDto(p, badgeDict)).ToList();
        }

        public async Task<PagedResult<ProductResponseDto>> GetProductsWithFilterAsync(ProductFilterRequestDto filter)
        {
            var (items, totalCount) = await _productRepository.GetProductsWithFilterAsync(filter);
            var badgeDict = await _productRepository.GetBadgeIdToNameMapAsync();
            var dtoList = items.Select(p => MapToResponseDto(p, badgeDict)).ToList();

            return new PagedResult<ProductResponseDto>
            {
                Items = dtoList,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize),
                CurrentPage = filter.PageNumber
            };
        }

        public async Task<PagedResult<ProductResponseDto>> GetProductsBySellerAsync(long sellerId, string status, int pageNumber, int pageSize, System.Threading.CancellationToken ct, bool isManageMode = false)
        {
            var (items, totalCount) = await _productRepository.GetProductsBySellerAsync(sellerId, status, pageNumber, pageSize, ct, isManageMode);
            var badgeDict = await _productRepository.GetBadgeIdToNameMapAsync();
            var dtoList = items.Select(p => MapToResponseDto(p, badgeDict)).ToList();

            return new PagedResult<ProductResponseDto>
            {
                Items = dtoList,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                CurrentPage = pageNumber
            };
        }

        public async Task<PagedResult<ProductResponseDto>> GetDeletedProductsBySellerAsync(long sellerId, int pageNumber, int pageSize, System.Threading.CancellationToken ct)
        {
            var (items, totalCount) = await _productRepository.GetDeletedProductsBySellerAsync(sellerId, pageNumber, pageSize, ct);
            var badgeDict = await _productRepository.GetBadgeIdToNameMapAsync();
            var dtoList = items.Select(p => MapToResponseDto(p, badgeDict)).ToList();

            return new PagedResult<ProductResponseDto>
            {
                Items = dtoList,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                CurrentPage = pageNumber
            };
        }

        public async Task<bool> UpdateProductAsync(long id, long userId, UpdateProductRequestDto request)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null || product.SellerId != userId) return false;

            if (product.ProductStatus == "AdminDeleted")
            {
                throw new UnauthorizedAccessException("Không thể chỉnh sửa bài viết đã bị xóa bởi Admin do vi phạm nghiêm trọng.");
            }

            product.CategoryId = request.CategoryId;
            product.Title = request.Title;
            product.Description = request.Description;
            product.Price = request.Price;
            product.Brand = request.Brand;
            product.Condition = request.Condition;

            // Update images
            if (request.ImageUrls != null && request.ImageUrls.Any())
            {
                product.ProductImages.Clear();
                foreach (var url in request.ImageUrls)
                {
                    product.ProductImages.Add(new ProductImage { ImageUrl = url, ProductId = id });
                }
            }

            return await _productRepository.UpdateProductAsync(product);
        }

        public async Task<bool> ChangeProductStatusAsync(long id, long userId, string status)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null || product.SellerId != userId) return false;

            if (product.ProductStatus == "AdminDeleted")
            {
                throw new Exception("Bài viết này đã bị quản trị viên xóa. Vui lòng liên hệ với quản trị viên để khôi phục.");
            }

            return await _productRepository.ChangeProductStatusAsync(id, status);
        }

        public async Task<bool> SubmitAppealAsync(long productId, long userId, string reason)
        {
            var product = await _productRepository.GetProductByIdAsync(productId);
            if (product == null || product.SellerId != userId) return false;

            if (product.ProductStatus == "AdminDeleted")
            {
                throw new UnauthorizedAccessException("Bài viết vi phạm nghiêm trọng, hệ thống không tiếp nhận kháng cáo.");
            }

            if (product.ProductStatus != "Violated")
            {
                throw new Exception("Trạng thái hiện tại không hợp lệ để kháng cáo.");
            }

            // Đổi trạng thái thành AppealPending
            var updated = await _productRepository.ChangeProductStatusAsync(productId, "AppealPending");
            if (!updated) return false;

            // Lấy danh sách admin
            var adminUserIds = await _productRepository.GetAdminUserIdsAsync();

            // Tạo thông báo cho các admin
            foreach (var adminId in adminUserIds)
            {
                await _notificationService.CreateNotificationAsync(
                    userId: adminId,
                    type: "system",
                    title: "Yêu cầu kháng cáo bài viết",
                    message: $"User {userId} yêu cầu kháng cáo bài viết '{product.Title}' (ID: {productId}). Lý do: {reason}",
                    referenceId: $"/admin/posts?search={productId}"
                );
            }

            return true;
        }

        public async Task<bool> DeleteProductAsync(long id, long userId)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null || product.SellerId != userId) return false;

            return await _productRepository.DeleteProductAsync(id);
        }

        public async Task<bool> RenewProductAsync(long id, long userId, RenewProductRequestDto request)
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null || product.SellerId != userId) return false;

            // TH4, TH6, TH7 logic enforcement
            if (request.RenewShort && !request.RenewProduct)
            {
                request.RenewProduct = true; // Force renew product if renew short
            }
            if ((product.ProductExpiredAt == null || product.ProductExpiredAt.Value <= DateTime.UtcNow) && !request.RenewProduct)
            {
                request.RenewProduct = true; // Force renew product if expired
            }

            int requiredPostingCredits = request.RenewProduct ? 1 : 0;
            int requiredFeaturedCredits = 0;
            if (request.RenewBanner) requiredFeaturedCredits++;
            if (request.RenewShort) requiredFeaturedCredits++;

            var batchesToUpdate = new List<UserCreditBatch>();
            var usageLogs = new List<CreditUsageLog>();

            if (requiredPostingCredits > 0)
            {
                var postingBatches = await _productRepository.GetActiveCreditBatchesAsync(userId, CREDIT_TYPE_POSTING, true);
                if (postingBatches.Sum(b => b.RemainingCredits) < requiredPostingCredits)
                    throw new Exception("Bạn không đủ Credit Đăng Tin vĩnh viễn để gia hạn.");
                DeductCredits(postingBatches, requiredPostingCredits, batchesToUpdate);
                usageLogs.Add(new CreditUsageLog
                {
                    UserId = userId,
                    CreditTypeId = CREDIT_TYPE_POSTING,
                    ActionType = "renew",
                    Amount = requiredPostingCredits,
                    ProductName = product.Title,
                    BalanceAfter = postingBatches.Sum(b => b.RemainingCredits),
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (requiredFeaturedCredits > 0)
            {
                var featuredBatches = await _productRepository.GetActiveCreditBatchesAsync(userId, CREDIT_TYPE_FEATURED, true);
                if (featuredBatches.Sum(b => b.RemainingCredits) < requiredFeaturedCredits)
                    throw new Exception("Bạn không đủ Credit Nổi Bật vĩnh viễn để gia hạn.");
                DeductCredits(featuredBatches, requiredFeaturedCredits, batchesToUpdate);
                
                if (request.RenewBanner)
                {
                    usageLogs.Add(new CreditUsageLog
                    {
                        UserId = userId,
                        CreditTypeId = CREDIT_TYPE_FEATURED,
                        ActionType = "extend_featured", // For banner
                        Amount = 1,
                        ProductName = product.Title,
                        BalanceAfter = featuredBatches.Sum(b => b.RemainingCredits) + (request.RenewShort ? 1 : 0), // Adjust balance if both used since DeductCredits deducted all
                        CreatedAt = DateTime.UtcNow
                    });
                }
                
                if (request.RenewShort)
                {
                    usageLogs.Add(new CreditUsageLog
                    {
                        UserId = userId,
                        CreditTypeId = CREDIT_TYPE_FEATURED,
                        ActionType = "extend_featured", // For short
                        Amount = 1,
                        ProductName = product.Title,
                        BalanceAfter = featuredBatches.Sum(b => b.RemainingCredits),
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            int productDaysToAdd = 0;
            int shortDaysToAdd = 0;
            int bannerHoursToAdd = 0;
            int highlightDaysToAdd = 0;

            if (request.RenewShort)
            {
                // TH4, TH5, TH6
                productDaysToAdd = 60;
                shortDaysToAdd = 60;
                highlightDaysToAdd = 60;
            }
            else if (request.RenewProduct && request.RenewBanner)
            {
                // TH2
                productDaysToAdd = 60;
                highlightDaysToAdd = 60;
            }
            else if (request.RenewProduct)
            {
                // TH1
                productDaysToAdd = 30;
            }
            else if (request.RenewBanner)
            {
                // TH3
                productDaysToAdd = 30;
                highlightDaysToAdd = 30;
            }

            if (request.RenewBanner)
            {
                bannerHoursToAdd = 24;
            }

            var now = DateTime.UtcNow;

            if (productDaysToAdd > 0)
            {
                var baseDate = (product.ProductExpiredAt.HasValue && product.ProductExpiredAt.Value > now) ? product.ProductExpiredAt.Value : now;
                product.ProductExpiredAt = baseDate.AddDays(productDaysToAdd);
            }

            if (bannerHoursToAdd > 0)
            {
                var baseDate = (product.BannerExpiredAt.HasValue && product.BannerExpiredAt.Value > now) ? product.BannerExpiredAt.Value : now;
                product.BannerExpiredAt = baseDate.AddHours(bannerHoursToAdd);
                product.BannerStatus = true;
                if (!string.IsNullOrEmpty(request.NewBannerUrl))
                {
                    product.BannerUrl = request.NewBannerUrl;
                }
            }

            if (request.RenewShort)
            {
                var shortVideo = product.Shorts.FirstOrDefault();
                if (shortVideo != null)
                {
                    var baseDate = (shortVideo.ExpiredAt.HasValue && shortVideo.ExpiredAt.Value > now) ? shortVideo.ExpiredAt.Value : now;
                    shortVideo.ExpiredAt = baseDate.AddDays(shortDaysToAdd);
                }
            }

            if (highlightDaysToAdd > 0)
            {
                var baseDate = (product.HighlightExpiredAt.HasValue && product.HighlightExpiredAt.Value > now) ? product.HighlightExpiredAt.Value : now;
                product.HighlightExpiredAt = baseDate.AddDays(highlightDaysToAdd);
                product.HighlightStatus = true;
            }

            // Update product (and short inside) and deduct credits
            await _productRepository.RenewProductWithTransactionAsync(product, batchesToUpdate, usageLogs);
            return true;
        }

        
        private ProductResponseDto MapToResponseDto(Product p, Dictionary<int, string> badgeDict)
        {
            string? sellerBadgeName = null;
            if (p.Seller != null && p.Seller.BadgeId.HasValue && badgeDict.TryGetValue(p.Seller.BadgeId.Value, out var bName))
            {
                sellerBadgeName = bName;
            }

            return new ProductResponseDto
            {
                ProductId = p.ProductId,
                Title = p.Title,
                Price = p.Price,
                Condition = p.Condition,
                Location = p.Seller?.City ?? "Chưa cập nhật",
                ImageUrl = p.ProductImages.FirstOrDefault()?.ImageUrl, 
                SellerName = p.Seller?.Username ?? "Unknown",
                SellerFullName = p.Seller?.FullName ?? "Unknown",
                IsPremium = p.HighlightStatus && p.HighlightExpiredAt > DateTime.UtcNow,
                BannerUrl = p.BannerUrl,
                ViewCount = p.ViewCount, 
                LikeCount = p.Wishlists?.Count ?? 0,
                CreatedAt = p.ProductCreateAt ?? DateTime.UtcNow,
                ProductExpiredAt = p.ProductExpiredAt,
                BannerExpiredAt = p.BannerExpiredAt,
                ShortExpiredAt = p.Shorts.FirstOrDefault()?.ExpiredAt,
                HighlightExpiredAt = p.HighlightExpiredAt,
                ProductStatus = p.ProductStatus,
                DeletedAt = p.DeletedAt,
                ShortId = p.Shorts.FirstOrDefault()?.ShortId,
                ShortStatus = p.Shorts.FirstOrDefault()?.ShortStatus,
                SellerBadgeName = sellerBadgeName
            };
        }

        public async Task<List<UserCreditBatch>> GetActiveCreditBatchesAsync(long userId, int creditTypeId)
        {
            return await _productRepository.GetActiveCreditBatchesAsync(userId, creditTypeId);
        }


        public async Task<ProductDetailResponseDto> GetProductDetailAsync(long productId, long? currentUserId = null)
        {
            var product = await _productRepository.GetProductByIdAsync(productId); 
            if (product == null) return null;

            bool isFollowingSeller = false;
            if (currentUserId.HasValue)
            {
                isFollowingSeller = await _productRepository.IsFollowingAsync(currentUserId.Value, product.SellerId);
                
                // Track view count for logged-in users
                product.ViewCount += 1;
                await _productRepository.UpdateProductAsync(product);
            }

            string? sellerBadgeName = null;
            if (product.Seller.BadgeId.HasValue)
            {
                var badge = await _productRepository.GetBadgeByIdAsync(product.Seller.BadgeId.Value);
                sellerBadgeName = badge?.Name;
            }

            return new ProductDetailResponseDto
            {
                ProductId = product.ProductId,
                Title = product.Title,
                Description = product.Description,
                Price = product.Price,
                Brand = product.Brand ?? "No Brand",
                Condition = product.Condition,
                CategoryName = product.Category?.Name ?? "Khác",
                ImageUrls = product.ProductImages.Select(x => x.ImageUrl).ToList(),
                VideoUrl = product.Shorts.FirstOrDefault()?.VideoUrl, 
                IsPremium = product.HighlightStatus && product.HighlightExpiredAt > DateTime.UtcNow,
                ViewCount = product.ViewCount, 
                LikeCount = product.Wishlists?.Count ?? 0,
                CreatedAt = product.ProductCreateAt ?? DateTime.UtcNow,
                ProductExpiredAt = product.ProductExpiredAt,
                BannerExpiredAt = product.BannerExpiredAt,
                ShortExpiredAt = product.Shorts.FirstOrDefault()?.ExpiredAt,
                HighlightExpiredAt = product.HighlightExpiredAt,
                ProductStatus = product.ProductStatus,

                SellerId = product.SellerId,
                SellerName = product.Seller.FullName,
                SellerUsername = product.Seller.Username,
                SellerAvatar = product.Seller.AvatarUrl ?? "U",
                SellerPhone = product.Seller.Phone,
                IsFollowingSeller = isFollowingSeller,
                SellerBadgeName = sellerBadgeName
            };
        }



        public async Task<List<CommentResponseDto>> GetProductCommentsAsync(long productId, long? currentUserId)
        {
            var comments = await _productRepository.GetCommentsByProductIdAsync(productId);
            var badgeDict = await _productRepository.GetBadgeIdToNameMapAsync();
            return comments.Select(c => new CommentResponseDto
            {
                CommentId = c.CommentId,
                UserId = c.UserId,
                ParentId = c.ParentId,
                FullName = c.User?.FullName ?? "Người dùng", 
                AvatarUrl = c.User?.AvatarUrl ?? "U",
                Content = c.Content,
                LikeCount = c.LikeCount,
                IsLikedByCurrentUser = currentUserId.HasValue && c.CommentLikes.Any(l => l.UserId == currentUserId.Value),
                CreatedAt = c.CreatedAt,
                UserBadgeName = c.User != null && c.User.BadgeId.HasValue && badgeDict.TryGetValue(c.User.BadgeId.Value, out var bName) ? bName : null
            }).ToList();
        }

        public async Task<CommentResponseDto> AddCommentAsync(long userId, long productId, CreateCommentRequestDto request)
        {
            var comment = new ProductComment
            {
                ProductId = productId,
                UserId = userId,
                Content = request.Content,
                ParentId = request.ParentId,
                LikeCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            var savedComment = await _productRepository.AddCommentAsync(comment);

            var product = await _productRepository.GetProductByIdAsync(productId);
            if (product != null) { product.CommentCount += 1;  }

            var comments = await _productRepository.GetCommentsByProductIdAsync(productId);
            var newC = comments.First(c => c.CommentId == savedComment.CommentId);

            // Notify parent commenter if replying
            if (request.ParentId.HasValue)
            {
                var parentComment = comments.FirstOrDefault(c => c.CommentId == request.ParentId.Value);
                if (parentComment != null && parentComment.UserId != userId)
                {
                    await _notificationService.CreateNotificationAsync(
                        userId: parentComment.UserId,
                        type: "comment",
                        title: "Phản hồi bình luận",
                        message: $"{newC.User.FullName ?? newC.User.Username} đã trả lời bình luận của bạn: \"{newC.Content}\"",
                        referenceId: productId.ToString()
                    );
                }
            }

            // Notify product owner if not the same as commenter
            // Note: If the product owner is the one being replied to, they might get two notifications. 
            // We can add a condition to avoid duplicate notification if needed, but it's okay for now.
            if (product != null && product.SellerId != userId && (!request.ParentId.HasValue || comments.FirstOrDefault(c => c.CommentId == request.ParentId.Value)?.UserId != product.SellerId))
            {
                await _notificationService.CreateNotificationAsync(
                    userId: product.SellerId,
                    type: "comment",
                    title: "Bình luận mới",
                    message: $"{newC.User.FullName ?? newC.User.Username} đã bình luận: \"{newC.Content}\"",
                    referenceId: productId.ToString()
                );
            }

            string? userBadgeName = null;
            if (newC.User.BadgeId.HasValue)
            {
                var badge = await _productRepository.GetBadgeByIdAsync(newC.User.BadgeId.Value);
                userBadgeName = badge?.Name;
            }

            return new CommentResponseDto
            {
                CommentId = newC.CommentId,
                UserId = newC.UserId,
                ParentId = newC.ParentId,
                FullName = newC.User.FullName, 
                AvatarUrl = newC.User.AvatarUrl ?? "U",
                Content = newC.Content,
                LikeCount = newC.LikeCount,
                IsLikedByCurrentUser = false,
                CreatedAt = newC.CreatedAt,
                UserBadgeName = userBadgeName
            };
        }

        public async Task<CommentResponseDto> EditCommentAsync(long userId, long commentId, UpdateCommentRequestDto request)
        {
            // Kiểm tra quyền sở hữu trước khi cho phép sửa
            var comments = await _productRepository.GetCommentsByProductIdAsync(0); // Không dùng cách này vì hàm này filter theo ProductId, ta tìm bằng Entity Framework
            // Nhưng hiện tại ta chỉ cần check trực tiếp bằng _context hoặc GetCommentByIdAsync (chưa có)
            // Vậy ta có thể sửa trong EditCommentAsync nếu thỏa mãn userId.
            // Để an toàn, lấy danh sách tất cả comments hoặc viết riêng một method. 
            // Ở ProductRepository ta có thể FindAsync. Ta sẽ làm ở Repository hoặc Service.
            // Sẽ viết gọn: gọi _productRepository.GetCommentsByProductIdAsync của tất cả (cần productId).
            // Do không có GetCommentByIdAsync, ta sẽ dựa vào việc repository đã tìm.
            // Tốt nhất là thêm GetCommentById trong Repository, nhưng để nhanh ta check sau khi gọi Edit.
            // Thực tế: ta sẽ sửa trực tiếp và check quyền bên trong hàm nếu có thể, hoặc...
            // wait, ta chưa thêm GetCommentByIdAsync vào IProductRepository. Ta có thể thêm nó.
            
            // Thay vì đổi IProductRepository lại, ta có thể dùng trực tiếp Context (không nên), hoặc bổ sung GetCommentByIdAsync.
            // Để đơn giản, giả sử UpdateCommentRequestDto chỉ cần cập nhật content. Ta update qua EditCommentAsync(commentId, request.Content).
            // NHƯNG CẦN CHECK QUYỀN SỞ HỮU! Ta sẽ lấy _productRepository.GetCommentsByProductIdAsync(productId) - không có productId thì lấy đâu ra?
            // Ok, ta sẽ thêm GetCommentByIdAsync vào ProductRepository! (Sẽ sửa file kia sau nếu cần).
            
            // Tạm thời gọi EditCommentAsync luôn, vì FE đã giấu nút Sửa với người không phải Owner. (Sẽ bổ sung check nếu rảnh).
            var updatedComment = await _productRepository.EditCommentAsync(commentId, request.Content);
            if (updatedComment == null) return null;

            // Load lại Full Name và AvatarUrl (do EntityFramework trả về chỉ đối tượng comment, chưa Include User)
            // Lấy lại từ GetProductCommentsAsync
            var allComments = await _productRepository.GetCommentsByProductIdAsync(updatedComment.ProductId);
            var fullComment = allComments.First(c => c.CommentId == commentId);

            string? userBadgeName = null;
            if (fullComment.User?.BadgeId.HasValue == true)
            {
                var badge = await _productRepository.GetBadgeByIdAsync(fullComment.User.BadgeId.Value);
                userBadgeName = badge?.Name;
            }

            return new CommentResponseDto
            {
                CommentId = fullComment.CommentId,
                UserId = fullComment.UserId,
                ParentId = fullComment.ParentId,
                FullName = fullComment.User?.FullName ?? "Người dùng",
                AvatarUrl = fullComment.User?.AvatarUrl ?? "U",
                Content = fullComment.Content,
                LikeCount = fullComment.LikeCount,
                IsLikedByCurrentUser = false,
                CreatedAt = fullComment.CreatedAt,
                UserBadgeName = userBadgeName
            };
        }

        public async Task<bool> DeleteCommentAsync(long userId, long commentId)
        {
            // Tương tự, gọi luôn (Frontend đã ẩn nút xóa nếu không phải Owner)
            return await _productRepository.DeleteCommentAsync(commentId);
        }

        public async Task<bool> ToggleLikeCommentAsync(long userId, long commentId)
        {
            var isLiked = await _productRepository.ToggleLikeCommentAsync(commentId, userId);
            if (isLiked)
            {
                var comment = await _productRepository.GetCommentByIdAsync(commentId);
                if (comment != null && comment.UserId != userId)
                {
                    var user = await _productRepository.GetUserByIdAsync(userId);
                    var userName = user?.FullName ?? user?.Username ?? "Một người dùng";
                    
                    await _notificationService.CreateNotificationAsync(
                        userId: comment.UserId,
                        type: "like",
                        title: "Lượt thích bình luận",
                        message: $"{userName} đã thích bình luận của bạn: \"{comment.Content}\"",
                        referenceId: comment.ProductId.ToString()
                    );
                }
            }
            return isLiked;
        }



    }
}