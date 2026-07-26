using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using REVORA_BE.Configs;
using REVORA_BE.Helpers;
using REVORA_BE.Hubs;
using REVORA_BE.Middlewares;
using REVORA_BE.Models;
using REVORA_BE.Data;
using REVORA_BE.Repositories.Implementations;
using REVORA_BE.Repositories.Interfaces;
using REVORA_BE.Security;
using REVORA_BE.Services;
using REVORA_BE.Services.Implementations;
using REVORA_BE.Services.Interfaces;
using REVORA_BE.Validations;
using REVORA_BE.Workers;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Add services to the container.
builder.Services.AddMemoryCache();

// Configure JWT Settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

builder.Services.AddControllers();

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// ======================== CẤU HÌNH DATABASE ĐỒNG BỘ POSTGRESQL ========================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Chỉ bật cơ chế tự động thử lại khi chạy trên môi trường AWS Production thực tế
        if (builder.Environment.IsProduction())
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,                       // Thử lại tối đa 5 lần
                maxRetryDelay: TimeSpan.FromSeconds(5), // Mỗi lần thử lại cách nhau 5 giây
                errorCodesToAdd: null
            );
        }
    });
});
// ======================================================================================

// Register Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISeedData, SeedData>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddHttpContextAccessor();

// Exception Handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings!.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Đăng ký Service & Repository
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddScoped<IPaidCreditPackageRepository, PaidCreditPackageRepository>();
builder.Services.AddScoped<IPaidCreditPackageService, PaidCreditPackageService>();

builder.Services.AddScoped<IUserCreditBatchRepository, UserCreditBatchRepository>();
builder.Services.AddScoped<ICreditPurchaseValidationService, CreditPurchaseValidationService>();
builder.Services.AddScoped<IUserCreditBatchService, UserCreditBatchService>();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPayOSService, PayOSService>();
builder.Services.AddScoped<CloudinaryService>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IShortRepository, ShortRepository>();
builder.Services.AddScoped<IShortService, ShortService>();

builder.Services.AddScoped<IAdminAuditLogRepository, AdminAuditLogRepository>();
builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.AddHostedService<REVORA_BE.Services.Implementations.NotificationBackgroundService>();
builder.Services.AddHostedService<MatchSessionCleanupWorker>();
builder.Services.AddHostedService<TrashCleanupWorker>();

builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddScoped<IMatchTradeRepository, MatchTradeRepository>();
builder.Services.AddScoped<IMatchTradeService, MatchTradeService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();

// Đăng ký FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.Configure<PayOSSettings>(builder.Configuration.GetSection("PayOSSettings"));
builder.Services.AddSignalR();

var app = builder.Build();

// ======================== KHỞI TẠO VÀ SEED DATABASE ĐỒNG BỘ ========================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        // Cả local và AWS hiện tại đều dùng PostgreSQL (Npgsql) nên sẽ chạy khối này để tự sinh bảng sạch sẽ
        if (context.Database.IsNpgsql())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.MigrateAsync();
        }

        // Chạy bộ khởi tạo Seed của ISeedData
        var initializer = services.GetRequiredService<ISeedData>();
        await initializer.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Đã xảy ra lỗi nghiêm trọng trong quá trình tự động tạo bảng hoặc Seed dữ liệu.");
    }
}
// ======================================================================================

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chathub");

app.Run();