<div align="center">

# 🛍️ REVORA — Next-Gen Second-Hand Fashion Marketplace & Trading Platform

**Nền tảng Thương mại Điện tử kết nối Mua bán & Trao đổi thời trang Second-Hand thời gian thực**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EF__Core-8.0-388E3C?style=for-the-badge&logo=nuget&logoColor=white)](https://docs.microsoft.com/ef/core/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![SignalR](https://img.shields.io/badge/SignalR-Realtime__Chat-0078D4?style=for-the-badge&logo=microsoft&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)
[![PayOS](https://img.shields.io/badge/PayOS-VietQR__Payment-00C853?style=for-the-badge&logo=contactlesspayment&logoColor=white)](https://payos.vn/)

</div>

---

## 📖 Giới thiệu dự án (About The Project)

**REVORA** là nền tảng thương mại điện tử chuyên biệt dành cho thị trường thời trang second-hand (đồ đã qua sử dụng/tái chế), hướng tới việc xây dựng cộng đồng thời trang bền vững. 

Khác với các sàn thương mại điện tử truyền thống chỉ tập trung vào mua bán một chiều, **REVORA** mang đến những trải nghiệm tương tác đổi mới và thú vị:
* 🔄 **Match & Trade (Ghép đôi & Trao đổi đồ):** Cơ chế quẹt thẻ (Tinder-like Swipe) độc đáo cho phép người dùng tìm kiếm những món đồ ưng ý và thực hiện trao đổi hàng hóa trực tiếp mà không bắt buộc phải dùng tiền mặt.
* 📱 **Video Shorts:** Không gian chia sẻ video review ngắn trực quan, sinh động giúp người mua đánh giá chân thực nhất tình trạng sản phẩm trước khi chốt đơn.
* 💬 **Realtime Chat & Negotiation:** Khung trò chuyện thời gian thực tích hợp công cụ thương lượng giá, gửi đề nghị trao đổi (Trade Offer) và chốt giao dịch ngay tức thì.

---

## ✨ Tính năng nổi bật (Key Features)

### 1. 🔄 Match & Trade (Ghép đôi & Trao đổi hàng hóa)
* **Quẹt khám phá (Swipe Left/Right):** Khám phá các sản phẩm thời trang theo cơ chế quẹt thẻ trực quan. Thích (Swipe Right) hoặc Bỏ qua (Swipe Left).
* **Ghép đôi thành công (It's a Match!):** Khi hai người dùng cùng sở hữu món đồ mà đối phương yêu thích -> Hệ thống tự động mở phiên kết nối (Match Session).
* **Thương lượng trao đổi:** Cho phép gửi đề nghị trao đổi sản phẩm, tùy chọn phụ phí bù tiền (nếu giá trị hai món đồ chênh lệch) ngay trong phòng trò chuyện.

### 2. 📱 Video Shorts (Mạng xã hội video ngắn)
* Đăng tải, phát video review sản phẩm chất lượng cao (lưu trữ và tối ưu hóa streaming qua **Cloudinary CDN**).
* Tương tác mạng xã hội: Thả tim (Like), Bình luận (Comment), Đăng ký theo dõi người sáng tạo (Follow Creator).
* **Shoppable Video:** Gắn thẻ (Tag) sản phẩm mua bán trực tiếp lên video Short giúp người xem mua hàng hoặc xin trao đổi chỉ với 1 cú nhấp chuột.

### 3. 💬 Realtime Chat & WebSocket (Trò chuyện thời gian thực)
* Tích hợp **ASP.NET Core SignalR** đảm bảo tin nhắn, thông báo và trạng thái cuộc trò chuyện được cập nhật tức thì với độ trễ cực thấp.
* Hỗ trợ gửi hình ảnh, thông tin sản phẩm và các yêu cầu thương lượng/trả giá ngay bên trong luồng chat.

### 4. 🛒 E-Commerce Marketplace (Sàn mua bán tiêu chuẩn)
* **Quản lý sản phẩm:** Đăng tin bán đồ với đa dạng danh mục (Quần áo, Giày dép, Túi xách, Phụ kiện, Đồng hồ...), phân loại tình trạng (New, LikeNew, Used) và gán nhãn nổi bật.
* **Tìm kiếm & Bộ lọc nâng cao:** Lọc theo mức giá, thương hiệu, độ mới, danh mục và từ khóa.
* **Tương tác người dùng:** Thêm vào Danh sách yêu thích (Wishlist), Theo dõi cửa hàng/người bán (Follow), Đánh giá và bình luận sản phẩm.

### 5. 💳 Hệ thống tín dụng (Credits) & Cổng thanh toán PayOS
* **Quà tặng tân thủ:** Tự động tặng **2 Free Posting Credits** cho người dùng mới đăng nhập lần đầu tiên.
* **Các loại Credit:**
  * *Posting Credits:* Dùng để đăng tin sản phẩm mới lên sàn.
  * *Featured Credits:* Dùng để đẩy tin, ghim sản phẩm lên vị trí nổi bật (Highlight/Banner).
* **Thanh toán VietQR tiện lợi:** Tích hợp cổng thanh toán **PayOS** (hỗ trợ quét mã QR qua tất cả các ứng dụng ngân hàng tại Việt Nam) để nạp gói tín dụng nhanh chóng, tự động cập nhật số dư qua Webhook.

### 6. 🔐 Bảo mật & Phân quyền nâng cao (Authentication & Authorization)
* **Đa dạng phương thức đăng nhập:**
  * Đăng nhập truyền thống bằng Email/Password (mật khẩu được băm bảo mật bằng **BCrypt**).
  * Đăng nhập nhanh bằng **Google OAuth 2.0**.
* **JWT & Refresh Token:** Quản lý phiên đăng nhập an toàn, lưu trữ Refresh Token trong **HttpOnly Cookie** chống tấn công XSS/CSRF.
* **RBAC & PBAC (Role & Permission-Based Access Control):** Hệ thống phân quyền chi tiết tới từng hành động (Create, Update, Delete, Admin Management...) bằng Custom Authorization Handler & Policy Provider.

### 7. 📊 Admin Dashboard (Hệ thống quản trị)
* Quản lý người dùng, khóa/mở khóa tài khoản, phân quyền quản trị viên.
* Kiểm duyệt sản phẩm, video shorts, danh mục và các gói tín dụng (Credit Packages).
* Theo dõi doanh thu, lịch sử mua gói tín dụng, audit log (nhật ký thao tác hệ thống) và thống kê hoạt động sàn realtime.

---

## 🛠️ Công nghệ sử dụng (Tech Stack)

| Thành phần | Công nghệ / Thư viện |
| :--- | :--- |
| **Framework chính** | .NET 8.0 (ASP.NET Core Web API) |
| **ORM & Database** | Entity Framework Core 8.0, PostgreSQL / MS SQL Server |
| **Realtime Engine** | ASP.NET Core SignalR (WebSockets / Long Polling) |
| **Media Storage & CDN**| CloudinaryDotNet (Lưu trữ ảnh, tối ưu hóa Video Shorts) |
| **Cổng thanh toán** | PayOS SDK (.NET VietQR Payment Gateway) |
| **Xác thực & Bảo mật** | JWT Bearer, Google.Apis.Auth, BCrypt.Net-Next, Custom Policy/Claims |
| **Validation & Mapping** | FluentValidation |
| **Tài liệu API** | Swashbuckle (Swagger UI / OpenAPI 3.0) |
| **DevOps / Container** | Docker, Docker Compose, Multi-stage Builds |

---

## 📁 Cấu trúc kiến trúc dự án (Project Structure)

```text
REVORA_BE/
│
├── 📂 DTOs/                # Data Transfer Objects (Request/Response models cho API)
├── 📂 Enums/               # Các định nghĩa kiểu trạng thái (OrderStatus, TradeMatchStatus...)
├── 📂 Exceptions/          # Global Exception Handling & Custom Business Exceptions
├── 📂 Helpers/             # Constants, Cloudinary/PayOS Settings & Utility helpers
├── 📂 Hubs/                # SignalR WebSocket Hubs (ChatHub.cs)
├── 📂 Middlewares/         # Custom Middlewares (GlobalExceptionHandlerMiddleware)
├── 📂 Migrations/          # EF Core Database Migrations & Snapshot
├── 📂 Models/              # Database Entities (User, Product, MatchSession, TradeMatch, Short...)
├── 📂 Repositories/        # Repository Pattern implementation (Truy xuất & thao tác dữ liệu)
├── 📂 Security/            # Permission Policies, Attributes & Authorization Handlers
├── 📂 Services/            # Business Logic Layer (MatchTradeService, ShortService, PayOSService...)
├── 📂 Validations/         # FluentValidation Rules cho Request DTOs
├── 📂 Workers/             # Background Services & Cron Jobs (Dọn dẹp rác, Match session hết hạn)
├── 📂 docs/                # Tài liệu nội bộ (Business Rules, Auth Context, Security Decisions)
│
├── 📄 AppDbContext.cs      # EF Core Database Context & Model Configurations
├── 📄 Program.cs           # Entry Point, Dependency Injection & Middleware Pipeline Config
└── 📄 Dockerfile           # Docker container configuration
```

---

## 🚀 Hướng dẫn cài đặt và chạy dự án (Getting Started)

### 1. Yêu cầu hệ thống (Prerequisites)
* [SDK .NET 8.0+](https://dotnet.microsoft.com/download/dotnet/8.0)
* [PostgreSQL](https://www.postgresql.org/download/) (Hoặc SQL Server / Docker Container)
* ID IDE khuyến nghị: Visual Studio 2022 / JetBrains Rider / VS Code.

### 2. Các bước cài đặt (Installation)

**Bước 1: Clone kho lưu trữ**
```bash
git clone https://github.com/Hoang175/REVORA_Backend.git
cd REVORA_Backend/REVORA_BE
```

**Bước 2: Cấu hình biến môi trường / User Secrets**
Tuyệt đối không lưu trữ Secret Keys thật trực tiếp trong file `appsettings.json`. Hãy sử dụng công cụ **User Secrets** của .NET hoặc thiết lập biến môi trường (Environment Variables):

```bash
# Khởi tạo User Secrets cho dự án
dotnet user-secrets init

# Cấu hình Database Connection String
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=RevoraDB;Username=postgres;Password=YOUR_POSTGRES_PASSWORD;"

# Cấu hình Google OAuth
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_GOOGLE_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_GOOGLE_CLIENT_SECRET"

# Cấu hình Cloudinary (Lưu trữ ảnh & video)
dotnet user-secrets set "CloudinarySettings:CloudName" "YOUR_CLOUDINARY_CLOUD_NAME"
dotnet user-secrets set "CloudinarySettings:ApiKey" "YOUR_CLOUDINARY_API_KEY"
dotnet user-secrets set "CloudinarySettings:ApiSecret" "YOUR_CLOUDINARY_API_SECRET"

# Cấu hình Email SMTP (Gmail App Password)
dotnet user-secrets set "EmailSettings:Password" "YOUR_EMAIL_APP_PASSWORD"

# Cấu hình PayOS (Cổng thanh toán QR)
dotnet user-secrets set "PayOSSettings:ClientId" "YOUR_PAYOS_CLIENT_ID"
dotnet user-secrets set "PayOSSettings:ApiKey" "YOUR_PAYOS_API_KEY"
dotnet user-secrets set "PayOSSettings:ChecksumKey" "YOUR_PAYOS_CHECKSUM_KEY"
```

**Bước 3: Chạy Database Migration & Khởi tạo dữ liệu mẫu (Seeding Data)**
```bash
# Cập nhật schema database mới nhất
dotnet ef database update
```

**Bước 4: Khởi chạy dự án**
```bash
dotnet run --launch-profile "https"
```
* API Server sẽ chạy tại: `https://localhost:7001` hoặc `http://localhost:5242` (tùy theo `launchSettings.json`).
* Mở trình duyệt và truy cập tài liệu Swagger UI: **`http://localhost:5242/swagger`** để kiểm thử các endpoints API.

---

## 🐳 Chạy bằng Docker (Optional)

Nếu bạn muốn chạy toàn bộ ứng dụng nhanh chóng bằng Docker:
```bash
# Build Docker Image
docker build -t revora-backend -f Dockerfile .

# Run Docker Container
docker run -d -p 5242:8080 --name revora-api revora-backend
```

---

## 📚 Tài liệu tham khảo (Internal Documentation)
Các tài liệu phân tích nghiệp vụ và quyết định kỹ thuật chi tiết của dự án được lưu trữ trong thư mục `/docs`:
* 📑 [Quy tắc nghiệp vụ & Quyền hạn (Business Rules)](file:///d:/Documments/EXE%20code/REVORA_BE/REVORA_BE/docs/business-rules.md)
* 🔐 [Kiến trúc xác thực & Phân quyền (Auth Documentation)](file:///d:/Documments/EXE%20code/REVORA_BE/REVORA_BE/docs/auth_documentation.md)
* 🛡️ [Các quyết định về bảo mật hệ thống (Security Decisions)](file:///d:/Documments/EXE%20code/REVORA_BE/REVORA_BE/docs/security-decisions.md)
* 🤖 [Hướng dẫn chuẩn lập trình & Agent Instructions](file:///d:/Documments/EXE%20code/REVORA_BE/REVORA_BE/docs/agent-instructions.md)

---

<div align="center">

**Phát triển bởi Đào Huy Hoàng — Đồ án Thực tập / Tốt nghiệp EXE (2026)**  
*Nếu thấy dự án thú vị và hữu ích, hãy để lại 1 ⭐ trên GitHub nhé!*

</div>
