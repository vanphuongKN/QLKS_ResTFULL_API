using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using QLKSan.ApiClients;
using QLKSan.Infrastructure;

namespace QLKSan
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews(options =>
            {
                // API trả 401 → đăng xuất và quay về trang đăng nhập
                options.Filters.Add<ApiUnauthorizedFilter>();
                // Chống CSRF: mọi request POST/PUT/DELETE phải kèm antiforgery token (form tag helper tự sinh token)
                options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
            });

            // Đăng nhập bằng cookie; JWT nhận từ API được lưu trong cookie để gọi API
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Auth/Login";
                    options.LogoutPath = "/Auth/Logout";
                    options.AccessDeniedPath = "/Auth/AccessDenied";
                    options.Cookie.Name = "QLKSan.Auth";
                    options.Cookie.HttpOnly = true;
                    options.SlidingExpiration = false;
                });
            builder.Services.AddAuthorization();

            // Khoá mã hoá cookie đăng nhập: lưu ra thư mục cố định khi chạy trong container để khởi động lại không bị đăng xuất
            var keysPath = builder.Configuration["DataProtection:KeysPath"];
            if (!string.IsNullOrWhiteSpace(keysPath))
                builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath)).SetApplicationName("QLKSan.Web");

            // Web không truy cập CSDL: mọi dữ liệu lấy từ QLKSan.API qua các typed HttpClient.
            // JwtTokenHandler tự gắn Bearer token của người dùng đang đăng nhập.
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddTransient<JwtTokenHandler>();
            builder.Services.AddTransient<ForwardedClientHandler>();
            var apiBaseUrl = new Uri(builder.Configuration["ApiSettings:BaseUrl"]);
            // Ảnh tải lên do trình duyệt tải trực tiếp từ API: dùng địa chỉ công khai nếu Web gọi API qua địa chỉ nội bộ (vd: Docker)
            var publicUrl = builder.Configuration["ApiSettings:PublicUrl"];
            MediaUrl.Configure(string.IsNullOrWhiteSpace(publicUrl) ? apiBaseUrl : new Uri(publicUrl));
            void AddApiClient<TClient, TImplementation>()
                where TClient : class
                where TImplementation : class, TClient
            {
                builder.Services.AddHttpClient<TClient, TImplementation>(c => c.BaseAddress = apiBaseUrl)
                    .AddHttpMessageHandler<JwtTokenHandler>()
                    .AddHttpMessageHandler<ForwardedClientHandler>();
            }

            AddApiClient<IAuthApiClient, AuthApiClient>();
            AddApiClient<IRoomApiClient, RoomApiClient>();
            AddApiClient<IRoomTypeApiClient, RoomTypeApiClient>();
            AddApiClient<IServiceApiClient, ServiceApiClient>();
            AddApiClient<IStaffApiClient, StaffApiClient>();
            AddApiClient<IPromotionApiClient, PromotionApiClient>();
            AddApiClient<IReviewApiClient, ReviewApiClient>();
            AddApiClient<IDashboardApiClient, DashboardApiClient>();
            AddApiClient<IAuditLogApiClient, AuditLogApiClient>();
            AddApiClient<ISettingsApiClient, SettingsApiClient>();
            AddApiClient<IAmenityApiClient, AmenityApiClient>();
            AddApiClient<IBookingApiClient, BookingApiClient>();
            AddApiClient<IFrontDeskApiClient, FrontDeskApiClient>();
            AddApiClient<IGuestRequestApiClient, GuestRequestApiClient>();
            AddApiClient<ICustomerApiClient, CustomerApiClient>();
            AddApiClient<IInvoiceApiClient, InvoiceApiClient>();
            AddApiClient<IHousekeepingApiClient, HousekeepingApiClient>();
            AddApiClient<IMaintenanceApiClient, MaintenanceApiClient>();
            AddApiClient<IReportApiClient, ReportApiClient>();
            AddApiClient<INightAuditApiClient, NightAuditApiClient>();
            AddApiClient<IPricingRuleApiClient, PricingRuleApiClient>();
            AddApiClient<IHotelApiClient, HotelApiClient>();
            AddApiClient<IChatApiClient, ChatApiClient>();

            // Thông tin khách sạn dùng chung cho footer và trang chủ (lưu đệm trong bộ nhớ)
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<HotelInfoProvider>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
