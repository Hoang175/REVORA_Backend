using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using REVORA_BE.Configs;
using REVORA_BE.DTOs;
using REVORA_BE.Exceptions;
using REVORA_BE.Models;
using REVORA_BE.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace REVORA_BE.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly JwtSettings _jwtSettings;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

        public AuthService(
            AppDbContext context,
            IJwtService jwtService,
            IOptions<JwtSettings> jwtSettings,
            IRefreshTokenService refreshTokenService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthService> logger,
            INotificationService notificationService,
            IEmailService emailService,
            Microsoft.Extensions.Configuration.IConfiguration configuration,
            Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _context = context;
            _jwtService = jwtService;
            _jwtSettings = jwtSettings.Value;
            _refreshTokenService = refreshTokenService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _notificationService = notificationService;
            _emailService = emailService;
            _configuration = configuration;
            _cache = cache;
        }

        public async Task SendRegistrationLinkAsync(string email, string verificationUrlTemplate, CancellationToken cancellationToken = default)
        {
            var existingUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (existingUser != null)
            {
                throw new ConflictException("Email này đã được sử dụng. Vui lòng chọn email khác.");
            }

            var token = Guid.NewGuid().ToString("N");

            // Save Token to cache for 15 minutes, state is "PENDING"
            var cacheKey = $"RegisterToken_{email}";
            Microsoft.Extensions.Caching.Memory.CacheExtensions.Set(_cache, cacheKey, token, TimeSpan.FromMinutes(15));

            var verificationUrl = verificationUrlTemplate.Replace("{email}", Uri.EscapeDataString(email)).Replace("{token}", Uri.EscapeDataString(token));

            var subject = "Xác nhận đăng ký tài khoản REVORA";
            var body = $@"
                <h3>Xin chào,</h3>
                <p>Cảm ơn bạn đã đăng ký tài khoản REVORA.</p>
                <p>Vui lòng click vào đường link bên dưới để xác nhận email của bạn:</p>
                <p><a href='{verificationUrl}' target='_blank'><b>Xác nhận Email</b></a></p>
                <p>Link này sẽ hết hạn trong 15 phút.</p>
                <p>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này.</p>
                <p>Trân trọng,<br/>Đội ngũ REVORA</p>";

            await _emailService.SendEmailAsync(email, subject, body);
        }

        public Task<bool> VerifyLinkAsync(string email, string token, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"RegisterToken_{email}";
            if (Microsoft.Extensions.Caching.Memory.CacheExtensions.TryGetValue(_cache, cacheKey, out string? storedValue))
            {
                if (storedValue == token)
                {
                    // Update cache to VERIFIED state
                    Microsoft.Extensions.Caching.Memory.CacheExtensions.Set(_cache, cacheKey, "VERIFIED", TimeSpan.FromMinutes(15));
                    return Task.FromResult(true);
                }
                else if (storedValue == "VERIFIED")
                {
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }

        public Task<bool> CheckVerificationStatusAsync(string email, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"RegisterToken_{email}";
            if (Microsoft.Extensions.Caching.Memory.CacheExtensions.TryGetValue(_cache, cacheKey, out string? storedValue) && storedValue == "VERIFIED")
            {
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public async Task RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
        {
            // Verify token status
            var cacheKey = $"RegisterToken_{dto.Email}";
            if (!Microsoft.Extensions.Caching.Memory.CacheExtensions.TryGetValue(_cache, cacheKey, out string? storedValue) || storedValue != "VERIFIED")
            {
                throw new ValidationException("Email chưa được xác thực hoặc phiên đăng ký đã hết hạn.");
            }

            // Remove Token from cache after successful registration
            _cache.Remove(cacheKey);

            // Optimized Uniqueness Check (1 query instead of 2)
            var existingUser = await _context.Users
                .AsNoTracking()
                .Where(u => u.Email == dto.Email || u.Username == dto.Username)
                .Select(u => new { u.Email, u.Username })
                .FirstOrDefaultAsync(cancellationToken);

            if (existingUser != null)
            {
                if (existingUser.Email == dto.Email)
                {
                    _logger.LogWarning("Registration failed: Email {Email} already exists", dto.Email);
                    throw new ConflictException(
                        clientMessage: "Email này đã được sử dụng. Vui lòng chọn email khác.",
                        internalMessage: $"Registration failed: Email {dto.Email} already exists",
                        code: "EmailAlreadyExists");
                }

                _logger.LogWarning("Registration failed: Username {Username} already exists", dto.Username);
                throw new ConflictException(
                    clientMessage: "Tên đăng nhập này đã tồn tại. Vui lòng chọn tên khác.",
                    internalMessage: $"Registration failed: Username {dto.Username} already exists",
                    code: "UsernameAlreadyExists");
            }

            // Load Default Role
            var userRole = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleName == "User", cancellationToken);

            if (userRole == null)
            {
                _logger.LogCritical("Critical Error: Default role 'User' not found in database.");
                throw new NotFoundException(
                    clientMessage: "Đã xảy ra lỗi cấu hình hệ thống. Vui lòng liên hệ quản trị viên.",
                    internalMessage: "Critical Error: Default role 'User' not found in database.",
                    code: "RoleNotFound");
            }

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                FullName = dto.FullName,
                City = dto.City,
                RoleId = userRole.RoleId,
                IsActive = true,
                IsFirstLogin = true,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("User {Username} registered successfully with ID {UserId}", user.Username, user.UserId);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Registration conflict for {Email}", dto.Email);
                throw new ConflictException(
                    clientMessage: "Đã có lỗi xung đột dữ liệu xảy ra khi đăng ký.",
                    internalMessage: $"Registration conflict in database write for Email {dto.Email}",
                    code: "RegistrationConflict");
            }
        }

        public async Task<TokenDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
        {
            // Load User with full Permission set
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Email == dto.Email || u.Username == dto.Email, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found {Email}", dto.Email);
                throw new UnauthorizedException(
                    clientMessage: "Tài khoản không tồn tại hoặc email/tên đăng nhập không chính xác.",
                    internalMessage: $"Login failed: User not found for {dto.Email}",
                    code: "UserNotFound");
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed: Invalid password for user {Email}", dto.Email);
                
                var errorData = new
                {
                    fullName = user.FullName,
                    avatarUrl = user.AvatarUrl,
                    username = user.Username
                };

                throw new UnauthorizedException(
                    clientMessage: "Mật khẩu không chính xác.",
                    internalMessage: $"Login failed: Invalid password for user {dto.Email}",
                    code: "InvalidPassword",
                    data: errorData);
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed: Account {Email} is disabled", dto.Email);
                throw new ForbiddenException(
                    clientMessage: "Tài khoản của bạn đã bị khóa hoặc tạm ngưng.",
                    internalMessage: $"Login failed: Account {dto.Email} is disabled",
                    code: "UserInactive");
            }

            // Extract Permissions for JWT Claims
            var permissions = user.Role?.RolePermissions?
                .Select(rp => rp.Permission.Name)
                .ToList() ?? new List<string>();

            bool isFirstLogin = false;
            if (user.IsFirstLogin)
            {
                isFirstLogin = true;
                user.IsFirstLogin = false;
                _context.Users.Update(user);

                var welcomeFreePack = await _context.FreeCreditPackages
                    .FirstOrDefaultAsync(f => f.Name == "Quà Tặng Tân Thủ", cancellationToken);

                if (welcomeFreePack != null)
                {
                    var newBatch = new UserCreditBatch
                    {
                        UserId = user.UserId,
                        CreditTypeId = welcomeFreePack.CreditTypeId,
                        FreePackageId = welcomeFreePack.FreeCreditPackageId,
                        RemainingCredits = welcomeFreePack.CreditAmount,
                        ClaimedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddDays(welcomeFreePack.DurationDays),
                        IsActive = true
                    };
                    await _context.UserCreditBatches.AddAsync(newBatch, cancellationToken);
                    
                    // Thêm thông báo
                    await _notificationService.CreateNotificationAsync(
                        userId: user.UserId,
                        type: "system",
                        title: "Chào mừng",
                        message: "Chào mừng bạn! Bạn được tặng 2 lượt đăng bài miễn phí."
                    );
                }

                var newbieBadge = await _context.Badges.FirstOrDefaultAsync(b => b.Name == "Eco Warrior", cancellationToken);
                if (newbieBadge != null)
                {
                    if (!await _context.UserBadges.AnyAsync(ub => ub.UserId == user.UserId && ub.BadgeId == newbieBadge.BadgeId, cancellationToken))
                    {
                        await _context.UserBadges.AddAsync(new UserBadge { UserId = user.UserId, BadgeId = newbieBadge.BadgeId }, cancellationToken);
                    }
                }
            }

            // Generate Tokens
            var accessToken = _jwtService.GenerateAccessToken(user, permissions);
            var refreshTokenString = _refreshTokenService.GenerateTokenString();

            // Standardized Metadata Extraction
            var context = _httpContextAccessor.HttpContext;
            var ipAddress = context?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var deviceName = context?.Request.Headers["User-Agent"].ToString() ?? "Unknown";

            // Persist Session
            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshTokenString,
                UserId = user.UserId,
                DeviceName = deviceName,
                IpAddress = ipAddress,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
            };

            await _context.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {Email} logged in successfully using {Device}", user.Email, deviceName);

            return new TokenDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenString,
                AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                RefreshTokenExpiresAt = refreshTokenEntity.ExpiresAt,
                IsFirstLogin = isFirstLogin
            };
        }

        public async Task<TokenDto> GoogleLoginAsync(REVORA_BE.DTOs.Request.GoogleLoginRequestDto request, CancellationToken cancellationToken = default)
        {
            var clientId = _configuration["Authentication:Google:ClientId"];
            Google.Apis.Auth.GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(request.IdToken, new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid Google ID Token");
                throw new UnauthorizedException(
                    clientMessage: "Đăng nhập bằng Google thất bại. Token không hợp lệ.",
                    internalMessage: "Google ValidateAsync failed",
                    code: "InvalidGoogleToken");
            }

            // Check if user exists
            var user = await _context.Users
                .Include(u => u.Role)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Email == payload.Email, cancellationToken);

            bool isFirstLogin = false;

            if (user == null)
            {
                // Register new user
                var userRole = await _context.Roles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.RoleName == "User", cancellationToken);

                if (userRole == null)
                {
                    throw new NotFoundException(
                        clientMessage: "Đã xảy ra lỗi cấu hình hệ thống.",
                        internalMessage: "Role 'User' not found.",
                        code: "RoleNotFound");
                }

                // Generate Username
                var emailPrefix = payload.Email.Split('@')[0];
                var baseUsername = emailPrefix.Length > 20 ? emailPrefix.Substring(0, 20) : emailPrefix;
                var finalUsername = baseUsername;
                int suffix = new Random().Next(1000, 9999);
                
                while (await _context.Users.AnyAsync(u => u.Username == finalUsername, cancellationToken))
                {
                    finalUsername = $"{baseUsername}_{suffix}";
                    suffix = new Random().Next(1000, 9999);
                }

                var defaultPassword = "12345678@";

                user = new User
                {
                    Username = finalUsername,
                    Email = payload.Email,
                    FullName = payload.Name ?? finalUsername,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
                    AvatarUrl = payload.Picture,
                    RoleId = userRole.RoleId,
                    IsActive = true,
                    IsFirstLogin = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync(cancellationToken);
                
                // Send email
                var emailBody = $@"
                    <h3>Xin chào {user.FullName},</h3>
                    <p>Tài khoản của bạn tại REVORA đã được tạo thành công thông qua Google.</p>
                    <p><b>Tên đăng nhập:</b> {user.Username}</p>
                    <p><b>Mật khẩu mặc định:</b> {defaultPassword}</p>
                    <p>Vui lòng đăng nhập và đổi mật khẩu sớm để bảo vệ tài khoản của bạn.</p>
                    <p>Trân trọng,<br/>Đội ngũ REVORA</p>";

                _ = _emailService.SendEmailAsync(user.Email, "Tài khoản REVORA đã được tạo", emailBody);

                // Reload user to get Role and Permissions
                user = await _context.Users
                    .Include(u => u.Role)
                        .ThenInclude(r => r!.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.UserId == user.UserId, cancellationToken);
            }
            else
            {
                if (!user.IsActive)
                {
                    throw new ForbiddenException(
                        clientMessage: "Tài khoản của bạn đã bị khóa hoặc tạm ngưng.",
                        internalMessage: $"Google login failed: Account {user.Email} is disabled",
                        code: "UserInactive");
                }

                if (!string.IsNullOrEmpty(payload.Picture) && user.AvatarUrl != payload.Picture)
                {
                    user.AvatarUrl = payload.Picture;
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            var permissions = user!.Role?.RolePermissions?
                .Select(rp => rp.Permission.Name)
                .ToList() ?? new List<string>();

            if (user.IsFirstLogin)
            {
                isFirstLogin = true;
                user.IsFirstLogin = false;
                _context.Users.Update(user);

                var welcomeFreePack = await _context.FreeCreditPackages
                    .FirstOrDefaultAsync(f => f.Name == "Quà Tặng Tân Thủ", cancellationToken);

                if (welcomeFreePack != null)
                {
                    var newBatch = new UserCreditBatch
                    {
                        UserId = user.UserId,
                        CreditTypeId = welcomeFreePack.CreditTypeId,
                        FreePackageId = welcomeFreePack.FreeCreditPackageId,
                        RemainingCredits = welcomeFreePack.CreditAmount,
                        ClaimedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddDays(welcomeFreePack.DurationDays),
                        IsActive = true
                    };
                    await _context.UserCreditBatches.AddAsync(newBatch, cancellationToken);
                    
                    await _notificationService.CreateNotificationAsync(
                        userId: user.UserId,
                        type: "system",
                        title: "Chào mừng",
                        message: "Chào mừng bạn! Bạn được tặng 2 lượt đăng bài miễn phí."
                    );
                }

                var newbieBadge = await _context.Badges.FirstOrDefaultAsync(b => b.Name == "Eco Warrior", cancellationToken);
                if (newbieBadge != null)
                {
                    if (!await _context.UserBadges.AnyAsync(ub => ub.UserId == user.UserId && ub.BadgeId == newbieBadge.BadgeId, cancellationToken))
                    {
                        await _context.UserBadges.AddAsync(new UserBadge { UserId = user.UserId, BadgeId = newbieBadge.BadgeId }, cancellationToken);
                    }
                }
            }

            var accessToken = _jwtService.GenerateAccessToken(user, permissions);
            var refreshTokenString = _refreshTokenService.GenerateTokenString();

            var context = _httpContextAccessor.HttpContext;
            var ipAddress = context?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var deviceName = context?.Request.Headers["User-Agent"].ToString() ?? "Unknown";

            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshTokenString,
                UserId = user.UserId,
                DeviceName = deviceName,
                IpAddress = ipAddress,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
            };

            await _context.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {Email} logged in via Google successfully", user.Email);

            return new TokenDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenString,
                AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                RefreshTokenExpiresAt = refreshTokenEntity.ExpiresAt,
                IsFirstLogin = isFirstLogin
            };
        }

        public async Task<TokenDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            // 1. Khởi tạo chiến lược thực thi (Execution Strategy) từ DbContext hiện tại
            var strategy = _context.Database.CreateExecutionStrategy();

            // 2. Bọc toàn bộ logic logic trong một delegate ném vào strategy.ExecuteAsync
            return await strategy.ExecuteAsync(async () =>
            {
                // 3. Khởi tạo transaction một cách an toàn bên trong khối bọc
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var oldToken = await _refreshTokenService.GetRefreshTokenAsync(refreshToken);

                    // 1. Invalid or Revoked check with 30s Grace Period for concurrent requests
                    if (oldToken == null || (oldToken.IsRevoked && (!oldToken.RevokedAt.HasValue || (DateTime.UtcNow - oldToken.RevokedAt.Value).TotalSeconds > 30)))
                    {
                        _logger.LogWarning("Refresh failed: Token is invalid or already revoked");
                        throw new UnauthorizedException(
                            clientMessage: "Phiên làm việc không hợp lệ hoặc đã kết thúc.",
                            internalMessage: "Refresh failed: Token is invalid or already revoked",
                            code: "InvalidRefreshToken");
                    }

                    // 2. Precise Expiration Check
                    if (oldToken.ExpiresAt < DateTime.UtcNow)
                    {
                        _logger.LogWarning("Refresh failed: Session has expired for token {Token}", refreshToken);
                        throw new UnauthorizedException(
                            clientMessage: "Phiên đăng nhập của bạn đã hết hạn. Vui lòng đăng nhập lại.",
                            internalMessage: $"Refresh failed: Session has expired for token {refreshToken}",
                            code: "SessionExpired");
                    }

                    var user = await _context.Users
                        .AsNoTracking()
                        .Include(u => u.Role)
                            .ThenInclude(r => r!.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                        .FirstOrDefaultAsync(u => u.UserId == oldToken.UserId, cancellationToken);

                    if (user == null || !user.IsActive)
                    {
                        _logger.LogWarning("Refresh failed: User {UserId} is inactive", oldToken.UserId);
                        throw new ForbiddenException(
                            clientMessage: "Tài khoản của bạn đã bị khóa hoặc tạm ngưng.",
                            internalMessage: $"Refresh failed: User {oldToken.UserId} is inactive or does not exist",
                            code: "UserInactive");
                    }

                    // 3. Token Rotation (Audit-safe update)
                    if (!oldToken.IsRevoked)
                    {
                        await _refreshTokenService.RevokeTokenAsync(oldToken);
                    }

                    // 4. Issue New Session
                    var newRefreshTokenString = _refreshTokenService.GenerateTokenString();
                    var context = _httpContextAccessor.HttpContext;
                    var ipAddress = context?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                    var deviceName = context?.Request.Headers["User-Agent"].ToString() ?? "Unknown";

                    var newRefreshToken = new RefreshToken
                    {
                        Token = newRefreshTokenString,
                        UserId = user.UserId,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                        IsRevoked = false,
                        DeviceName = deviceName,
                        IpAddress = ipAddress
                    };

                    await _context.RefreshTokens.AddAsync(newRefreshToken, cancellationToken);

                    var permissions = user.Role?.RolePermissions?
                        .Select(rp => rp.Permission.Name)
                        .ToList() ?? new List<string>();

                    var accessToken = _jwtService.GenerateAccessToken(user, permissions);

                    // 5. Atomic Commit
                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("Token rotated successfully for User {UserId}", user.UserId);

                    return new TokenDto
                    {
                        AccessToken = accessToken,
                        RefreshToken = newRefreshTokenString,
                        AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                        RefreshTokenExpiresAt = newRefreshToken.ExpiresAt,
                        IsFirstLogin = false
                    };
                }
                catch (Exception ex)
                {
                    if (_context.Database.CurrentTransaction != null)
                        await transaction.RollbackAsync(cancellationToken);

                    _logger.LogError(ex, "Transaction failed during token refresh");
                    throw;
                }
            }); // Kết thúc khối xử lý của Strategy
        }

        public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("Logout attempt with empty token");
                throw new UnauthorizedException(
                    clientMessage: "Phiên làm việc không hợp lệ hoặc đã kết thúc.",
                    internalMessage: "Logout attempt with empty token",
                    code: "InvalidRefreshToken");
            }

            var token = await _refreshTokenService.GetRefreshTokenAsync(refreshToken);

            if (token == null || token.IsRevoked)
            {
                _logger.LogWarning("Logout failed: Token is invalid or already revoked");
                throw new UnauthorizedException(
                    clientMessage: "Phiên làm việc không hợp lệ hoặc đã kết thúc.",
                    internalMessage: "Logout failed: Token is invalid or already revoked",
                    code: "InvalidRefreshToken");
            }

            if (token.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Logout failed: Session is already expired");
                throw new UnauthorizedException(
                    clientMessage: "Phiên đăng nhập của bạn đã hết hạn. Vui lòng đăng nhập lại.",
                    internalMessage: $"Logout failed: Session is already expired for token {refreshToken}",
                    code: "SessionExpired");
            }

            await _refreshTokenService.RevokeTokenAsync(token);

            await CleanupMatchSessionAsync(token.UserId);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} logged out successfully", token.UserId);
        }

        public async Task LogoutAllAsync(long userId, CancellationToken cancellationToken = default)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    // 1. Get all active tokens for this user
                    var activeTokens = await _context.RefreshTokens
                        .Where(t => t.UserId == userId && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
                        .ToListAsync(cancellationToken);

                    if (activeTokens.Any())
                    {
                        // 2. Bulk Revoke
                        foreach (var token in activeTokens)
                        {
                            token.IsRevoked = true;
                            token.RevokedAt = DateTime.UtcNow;
                        }

                        _context.RefreshTokens.UpdateRange(activeTokens);
                    }

                    await CleanupMatchSessionAsync(userId);

                    await _context.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                    _logger.LogInformation("User {UserId} logged out from all devices", userId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Transaction failed during LogoutAll for User {UserId}", userId);
                    throw;
                }
            });
        }

        public async Task ChangePasswordAsync(long userId, ChangePasswordDto dto, CancellationToken cancellationToken = default)
        {
            // 1. DTO Validation
            if (dto.NewPassword != dto.ConfirmPassword)
            {
                _logger.LogWarning("ChangePassword failed: New passwords do not match for User {UserId}", userId);
                throw new ValidationException(
                    clientMessage: "Xác nhận mật khẩu mới không trùng khớp.",
                    errors: new Dictionary<string, string[]>
                    {
                        { nameof(dto.ConfirmPassword), new[] { "Xác nhận mật khẩu mới không trùng khớp." } }
                    },
                    internalMessage: $"ChangePassword failed: New passwords do not match for User {userId}");
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                // 2. Begin Transaction to ensure atomic password update + session wipe
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    // 3. Load User
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
                    
                    // Refined exception mapping under constraint 3
                    if (user == null)
                    {
                        _logger.LogWarning("ChangePassword failed: User {UserId} not found", userId);
                        throw new NotFoundException(
                            clientMessage: "Tài khoản không tồn tại trên hệ thống.",
                            internalMessage: $"ChangePassword failed: User {userId} not found in database",
                            code: "UserNotFound");
                    }

                    if (!user.IsActive)
                    {
                        _logger.LogWarning("ChangePassword failed: User {UserId} is inactive", userId);
                        throw new ForbiddenException(
                            clientMessage: "Tài khoản của bạn đã bị khóa hoặc tạm ngưng.",
                            internalMessage: $"ChangePassword failed: User {userId} exists but is inactive",
                            code: "UserInactive");
                    }

                    // 4. Verify Old Password
                    if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                    {
                        _logger.LogWarning("ChangePassword failed: Wrong current password for User {UserId}", userId);
                        throw new ValidationException(
                            clientMessage: "Mật khẩu hiện tại không chính xác.",
                            errors: new Dictionary<string, string[]>
                            {
                                { nameof(dto.CurrentPassword), new[] { "Mật khẩu hiện tại không chính xác." } }
                            },
                            internalMessage: $"ChangePassword failed: Wrong current password for User {userId}");
                    }

                    // 4.1. Prevent Password Reuse: New password must differ from current
                    if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash))
                    {
                        _logger.LogWarning("ChangePassword failed: New password is identical to current password for User {UserId}", userId);
                        throw new ValidationException(
                            clientMessage: "Mật khẩu mới không được trùng với mật khẩu hiện tại.",
                            errors: new Dictionary<string, string[]>
                            {
                                { nameof(dto.NewPassword), new[] { "Mật khẩu mới không được trùng với mật khẩu hiện tại." } }
                            },
                            internalMessage: $"ChangePassword failed: New password is identical to current password for User {userId}");
                    }

                    // 4.2. Password must not contain the user's username or email local-part
                    if (user.Username.Length >= 3 && dto.NewPassword.ToLower().Contains(user.Username.ToLower()))
                    {
                        _logger.LogWarning("ChangePassword failed: Password contains username for User {UserId}", userId);
                        throw new ValidationException(
                            clientMessage: "Mật khẩu không được chứa tên đăng nhập của bạn.",
                            errors: new Dictionary<string, string[]>
                            {
                                { nameof(dto.NewPassword), new[] { "Mật khẩu không được chứa tên đăng nhập." } }
                            },
                            internalMessage: $"ChangePassword failed: Password contains username for User {userId}");
                    }

                    var emailLocalPart = user.Email.Split('@')[0];
                    if (emailLocalPart.Length >= 4 && dto.NewPassword.ToLower().Contains(emailLocalPart.ToLower()))
                    {
                        _logger.LogWarning("ChangePassword failed: Password contains email local-part for User {UserId}", userId);
                        throw new ValidationException(
                            clientMessage: "Mật khẩu không được chứa thông tin email của bạn.",
                            errors: new Dictionary<string, string[]>
                            {
                                { nameof(dto.NewPassword), new[] { "Mật khẩu không được chứa thông tin email." } }
                            },
                            internalMessage: $"ChangePassword failed: Password contains email local-part for User {userId}");
                    }

                    // 5. Update Password
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                    _context.Users.Update(user);

                    // 6. Security Enforcement: Revoke ALL sessions
                    var activeTokens = await _context.RefreshTokens
                        .Where(t => t.UserId == userId && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
                        .ToListAsync(cancellationToken);

                    if (activeTokens.Any())
                    {
                        foreach (var token in activeTokens)
                        {
                            token.IsRevoked = true;
                            token.RevokedAt = DateTime.UtcNow;
                        }
                        _context.RefreshTokens.UpdateRange(activeTokens);
                    }

                    // 7. Commit
                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("Password changed successfully for User {UserId}. All sessions revoked.", userId);
                }
                catch (Exception ex)
                {
                    if (_context.Database.CurrentTransaction != null)
                        await transaction.RollbackAsync(cancellationToken);

                    // Only log unexpected infrastructure errors.
                    // Business exceptions (BaseException) are already logged with LogWarning
                    // at their specific check points and do not constitute a system failure.
                    if (ex is not BaseException)
                        _logger.LogError(ex, "Unexpected error during password change for User {UserId}", userId);

                    throw;
                }
            });
        }

        private async Task CleanupMatchSessionAsync(long userId)
        {
            try
            {
                var activeSessions = await _context.MatchSessions
                    .Include(s => s.OfferingProducts)
                    .Where(s => s.UserId == userId && s.Status == "Active")
                    .ToListAsync();

                foreach (var session in activeSessions)
                {
                    // Cleanup swipes
                    var swipes = await _context.MatchSwipes
                        .Where(s => s.MatchSessionId == session.MatchSessionId)
                        .ToListAsync();
                    _context.MatchSwipes.RemoveRange(swipes);

                    // Cleanup notifications
                    var notifications = await _context.MatchInterestNotifications
                        .Where(n => n.MatchSessionId == session.MatchSessionId
                            || n.InterestedUserId == userId
                            || n.OwnerUserId == userId)
                        .ToListAsync();
                    _context.MatchInterestNotifications.RemoveRange(notifications);

                    session.Status = "Ended";
                    session.EndedAt = DateTime.UtcNow;
                }
            }

            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup match sessions for user {UserId}.", userId);
            }
        }

        public async Task SendResetPasswordOtpAsync(string email, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException("Email không tồn tại trong hệ thống");
            }

            var otp = new Random().Next(100000, 999999).ToString();
            var cacheKey = $"ResetPasswordOTP_{email}";

            Microsoft.Extensions.Caching.Memory.CacheExtensions.Set(_cache, cacheKey, otp, TimeSpan.FromMinutes(5));

            var subject = "REVORA - Mã xác nhận khôi phục mật khẩu";
            var message = $"Mã OTP xác nhận khôi phục mật khẩu của bạn là: {otp}. Mã này sẽ hết hạn trong 5 phút.";
            await _emailService.SendEmailAsync(email, subject, message);
        }

        public Task<bool> VerifyResetPasswordOtpAsync(string email, string otp, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"ResetPasswordOTP_{email}";
            if (Microsoft.Extensions.Caching.Memory.CacheExtensions.TryGetValue<string>(_cache, cacheKey, out var cachedOtp))
            {
                return Task.FromResult(cachedOtp == otp);
            }
            return Task.FromResult(false);
        }

        public async Task ResetPasswordWithOtpAsync(string email, string otp, string newPassword, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"ResetPasswordOTP_{email}";
            if (!Microsoft.Extensions.Caching.Memory.CacheExtensions.TryGetValue<string>(_cache, cacheKey, out var cachedOtp) || cachedOtp != otp)
            {
                throw new ValidationException("Mã OTP không hợp lệ hoặc đã hết hạn.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (user == null)
            {
                throw new NotFoundException("Email không tồn tại trong hệ thống");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync(cancellationToken);

            _cache.Remove(cacheKey);

            // Clean up old sessions
            await CleanupMatchSessionAsync(user.UserId);
        }
    }
}
