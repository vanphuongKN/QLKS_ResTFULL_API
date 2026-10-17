using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using QLKSan.Business;
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
                    // Enum trả về dạng chuỗi ("Available") thay vì số (0)
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    // TẠM THỜI: tránh lỗi vòng lặp khi trả Entity có navigation 2 chiều; bỏ khi đã chuyển hết sang DTO (Giai đoạn 2)
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                });
            builder.Services.AddOpenApi();

            builder.Services.AddScoped<IRoomService, RoomService>();
            builder.Services.AddScoped<IReceptionService, ReceptionService>();
            builder.Services.AddScoped<IServiceManager, ServiceManager>();
            builder.Services.AddScoped<ICheckoutService, CheckoutService>();
            builder.Services.AddScoped<IHousekeepingService, HousekeepingService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<ICustomerService, CustomerService>();

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            var app = builder.Build();

            app.ApplySchemaFixes();

            // Configure the HTTP request pipeline.
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

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
