using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Data;

namespace QLKSan
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Giữ nguyên tên thuộc tính PascalCase (MaPhong, CCCD_Passport) thay vì camelCase (maPhong, cccD_Passport)
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                    // Enum trả về dạng chuỗi ("Available") thay vì số (0)
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

            // Lỗi trả về dạng ProblemDetails (RFC 7807)
            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

            // Xác thực bằng JWT Bearer
            var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
            builder.Services.Configure<JwtSettings>(jwtSection);
            var jwtSettings = jwtSection.Get<JwtSettings>();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });
            builder.Services.AddAuthorization();

            // Chính sách khách sạn (giờ nhận/trả phòng, phụ thu, tiền cọc, tự huỷ khi khách không đến)
            builder.Services.Configure<HotelPolicy>(builder.Configuration.GetSection(HotelPolicy.SectionName));
            builder.Services.Configure<HotelInfo>(builder.Configuration.GetSection(HotelInfo.SectionName));
            builder.Services.Configure<LoyaltyPolicy>(builder.Configuration.GetSection(LoyaltyPolicy.SectionName));
            builder.Services.AddHostedService<NoShowCleanupService>();

            builder.Services.AddScoped<IRoomAvailabilityService, RoomAvailabilityService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<ITokenService, JwtTokenService>();
            builder.Services.AddScoped<IRoomService, RoomService>();
            builder.Services.AddScoped<IReceptionService, ReceptionService>();
            builder.Services.AddScoped<IServiceManager, ServiceManager>();
            builder.Services.AddScoped<ICheckoutService, CheckoutService>();
            builder.Services.AddScoped<IHousekeepingService, HousekeepingService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IPromotionService, PromotionService>();
            builder.Services.AddScoped<IReviewService, ReviewService>();
            builder.Services.AddScoped<IRoomTypeService, RoomTypeService>();
            builder.Services.AddScoped<IRoomTypeImageService, RoomTypeImageService>();
            builder.Services.AddScoped<ICustomerService, CustomerService>();
            builder.Services.AddScoped<IAccountingService, AccountingService>();
            builder.Services.AddScoped<IPricingService, PricingService>();

            // Ảnh quản trị tải lên: lưu ở wwwroot/uploads của API
            var media = new MediaStorage(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads"));
            builder.Services.AddSingleton(media);

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            var app = builder.Build();

            app.ApplySchemaFixes();

            // Configure the HTTP request pipeline.
            app.UseExceptionHandler();
            app.UseStatusCodePages();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/openapi/v1.json", "QLKSan API v1");
                    options.RoutePrefix = "swagger";
                });
            }

            app.UseHttpsRedirection();

            // Phục vụ công khai ảnh đã tải lên (Web hiển thị qua <API>/uploads/...)
            app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(media.RootPath), RequestPath = MediaStorage.UrlPrefix });

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
