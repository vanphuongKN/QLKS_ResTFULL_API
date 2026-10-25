using Microsoft.AspNetCore.Authentication.Cookies;
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

            // Web không truy cập CSDL: mọi dữ liệu lấy từ QLKSan.API qua các typed HttpClient.
            // JwtTokenHandler tự gắn Bearer token của người dùng đang đăng nhập.
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddTransient<JwtTokenHandler>();
            var apiBaseUrl = new Uri(builder.Configuration["ApiSettings:BaseUrl"]);
            MediaUrl.Configure(apiBaseUrl);
            void AddApiClient<TClient, TImplementation>()
                where TClient : class
                where TImplementation : class, TClient
            {
                builder.Services.AddHttpClient<TClient, TImplementation>(c => c.BaseAddress = apiBaseUrl)
                    .AddHttpMessageHandler<JwtTokenHandler>();
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
            AddApiClient<IReportApiClient, ReportApiClient>();
            AddApiClient<IPricingRuleApiClient, PricingRuleApiClient>();
            AddApiClient<IHotelApiClient, HotelApiClient>();

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
