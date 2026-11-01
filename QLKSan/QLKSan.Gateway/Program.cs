using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;

namespace QLKSan.Gateway
{
    // Cổng vào duy nhất của hệ thống: Web và trang khách gọi Gateway, Gateway chuyển tới service phụ trách
    // (định tuyến trong mục "ReverseProxy" của appsettings.json) và giới hạn tần suất gọi.
    // Không chứa nghiệp vụ; từng service vẫn tự xác thực và phân quyền.
    public class Program
    {
        public const string ChinhSachMacDinh = "mac-dinh";
        public const string ChinhSachDangNhap = "dang-nhap";
        public const string ChinhSachChat = "chat";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var jwt = builder.Configuration.GetSection("Jwt");
            var khoa = jwt["Key"];
            if (string.IsNullOrWhiteSpace(khoa) || khoa.Length < 32)
                throw new InvalidOperationException("Thiếu Jwt:Key (tối thiểu 32 ký tự, giống khoá của QLKSan.API). Đặt bằng biến môi trường Jwt__Key.");

            builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            // Đọc JWT (không bắt buộc) chỉ để giới hạn tần suất theo người dùng thay vì theo IP
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt["Issuer"],
                        ValidateAudience = true,
                        ValidAudience = jwt["Audience"],
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(khoa)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });

            // Giới hạn tần suất: đã đăng nhập tính theo người dùng, chưa đăng nhập theo IP; đăng nhập / đăng ký chặt hơn
            var gioiHan = builder.Configuration.GetSection("RateLimit");
            var moiPhut = gioiHan.GetValue("MoiPhut", 600);
            var dangNhapMoiPhut = gioiHan.GetValue("DangNhapMoiPhut", 20);
            var chatMoiPhut = gioiHan.GetValue("ChatMoiPhut", 20);
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, ct) =>
                {
                    context.HttpContext.Response.ContentType = "application/problem+json";
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        type = "https://tools.ietf.org/html/rfc6585#section-4",
                        title = "Quá nhiều yêu cầu",
                        status = 429,
                        detail = "Bạn thao tác quá nhanh, vui lòng thử lại sau ít phút."
                    }, ct);
                };
                options.AddPolicy(ChinhSachMacDinh, http => RateLimitPartition.GetFixedWindowLimiter(KhoaGioiHan(http),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = moiPhut, Window = TimeSpan.FromMinutes(1) }));
                // Trợ lý ảo có thể gọi mô hình ngôn ngữ trả phí: giới hạn riêng
                options.AddPolicy(ChinhSachChat, http => RateLimitPartition.GetFixedWindowLimiter("chat:" + KhoaGioiHan(http),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = chatMoiPhut, Window = TimeSpan.FromMinutes(1) }));
                options.AddPolicy(ChinhSachDangNhap, http => RateLimitPartition.GetFixedWindowLimiter("ip:" + http.Connection.RemoteIpAddress,
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = dangNhapMoiPhut, Window = TimeSpan.FromMinutes(1) }));
            });

            // IP thật của người dùng: Web gửi kèm X-Forwarded-For; chỉ tin loopback và mạng khai báo trong Gateway:MangTinCay
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                foreach (var mang in builder.Configuration.GetSection("Gateway:MangTinCay").Get<string[]>() ?? Array.Empty<string>())
                    if (!string.IsNullOrWhiteSpace(mang)) options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(mang.Trim()));
            });

            var app = builder.Build();

            app.UseForwardedHeaders();
            app.UseAuthentication();
            app.UseRateLimiter();

            // Gateway còn sống (sức khoẻ API lõi: /api/health, chuyển tiếp như các đường dẫn khác)
            app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "QLKSan.Gateway", time = DateTime.Now }));
            app.MapReverseProxy();

            app.Run();
        }

        private static string KhoaGioiHan(HttpContext http)
        {
            var nguoiDung = http.User.Identity?.IsAuthenticated == true
                ? http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? http.User.FindFirstValue("sub")
                : null;
            return nguoiDung != null ? "nd:" + nguoiDung : "ip:" + http.Connection.RemoteIpAddress;
        }
    }
}
