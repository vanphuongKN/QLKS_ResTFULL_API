using Microsoft.EntityFrameworkCore;
using QLKSan.Data;

namespace QLKSan
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddScoped<QLKSan.Business.IRoomService, QLKSan.Business.RoomService>();
            builder.Services.AddScoped<QLKSan.Business.IReceptionService, QLKSan.Business.ReceptionService>();
            builder.Services.AddScoped<QLKSan.Business.IServiceManager, QLKSan.Business.ServiceManager>();
            builder.Services.AddScoped<QLKSan.Business.ICheckoutService, QLKSan.Business.CheckoutService>();
            builder.Services.AddScoped<QLKSan.Business.IHousekeepingService, QLKSan.Business.HousekeepingService>();
            builder.Services.AddScoped<QLKSan.Business.IAdminService, QLKSan.Business.AdminService>();
            builder.Services.AddScoped<QLKSan.Business.ICustomerService, QLKSan.Business.CustomerService>();

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            var app = builder.Build();

            // TẠM THỜI: QLKSan.API đã chạy đoạn này (Data/DatabaseSchemaFixer.cs). Xoá khi Web không còn truy cập DB trực tiếp (Giai đoạn 5).
            // Dynamic Database Schema Fixes (e.g. drop duplicate unique constraints on nullable columns)
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    using (var command = context.Database.GetDbConnection().CreateCommand())
                    {
                        context.Database.OpenConnection();
                        command.CommandText = @"
                            DECLARE @ConstraintName NVARCHAR(200);
                            DECLARE @Sql NVARCHAR(MAX);

                            -- 1. KhachHang.MaDangNhap
                            SELECT @ConstraintName = tc.CONSTRAINT_NAME 
                            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                            JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu ON tc.CONSTRAINT_NAME = ccu.CONSTRAINT_NAME
                            WHERE tc.TABLE_NAME = 'KhachHang' AND tc.CONSTRAINT_TYPE = 'UNIQUE' AND ccu.COLUMN_NAME = 'MaDangNhap';
                            IF @ConstraintName IS NOT NULL
                            BEGIN
                                SET @Sql = 'ALTER TABLE KhachHang DROP CONSTRAINT ' + QUOTENAME(@ConstraintName);
                                EXEC sp_executesql @Sql;
                            END

                            -- 2. KhachHang.CCCD_Passport
                            SET @ConstraintName = NULL;
                            SELECT @ConstraintName = tc.CONSTRAINT_NAME 
                            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                            JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu ON tc.CONSTRAINT_NAME = ccu.CONSTRAINT_NAME
                            WHERE tc.TABLE_NAME = 'KhachHang' AND tc.CONSTRAINT_TYPE = 'UNIQUE' AND ccu.COLUMN_NAME = 'CCCD_Passport';
                            IF @ConstraintName IS NOT NULL
                            BEGIN
                                SET @Sql = 'ALTER TABLE KhachHang DROP CONSTRAINT ' + QUOTENAME(@ConstraintName);
                                EXEC sp_executesql @Sql;
                            END

                            -- 3. NhanVien.MaDangNhap
                            SET @ConstraintName = NULL;
                            SELECT @ConstraintName = tc.CONSTRAINT_NAME 
                            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                            JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu ON tc.CONSTRAINT_NAME = ccu.CONSTRAINT_NAME
                            WHERE tc.TABLE_NAME = 'NhanVien' AND tc.CONSTRAINT_TYPE = 'UNIQUE' AND ccu.COLUMN_NAME = 'MaDangNhap';
                            IF @ConstraintName IS NOT NULL
                            BEGIN
                                SET @Sql = 'ALTER TABLE NhanVien DROP CONSTRAINT ' + QUOTENAME(@ConstraintName);
                                EXEC sp_executesql @Sql;
                            END

                            -- 4. PhieuDatPhong.MaKhuyenMai Column
                            IF NOT EXISTS (
                                SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                                WHERE TABLE_NAME = 'PhieuDatPhong' AND COLUMN_NAME = 'MaKhuyenMai'
                            )
                            BEGIN
                                ALTER TABLE PhieuDatPhong ADD MaKhuyenMai VARCHAR(10) NULL;
                            END

                            -- 5. Fix Unicode: Convert VARCHAR to NVARCHAR for columns that may store Vietnamese text
                            IF EXISTS (
                                SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                                WHERE TABLE_NAME = 'TaiKhoan' AND COLUMN_NAME = 'TenDangNhap' AND DATA_TYPE = 'varchar'
                            )
                            BEGIN
                                ALTER TABLE TaiKhoan ALTER COLUMN TenDangNhap NVARCHAR(50) NOT NULL;
                            END

                            IF EXISTS (
                                SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                                WHERE TABLE_NAME = 'TaiKhoan' AND COLUMN_NAME = 'MatKhau' AND DATA_TYPE = 'varchar'
                            )
                            BEGIN
                                ALTER TABLE TaiKhoan ALTER COLUMN MatKhau NVARCHAR(255) NOT NULL;
                            END
                        ";
                        command.ExecuteNonQuery();
                    }
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine($"Error running db schema fix: {ex.Message}");
                }
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

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
