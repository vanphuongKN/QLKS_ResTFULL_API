namespace QLKSan.Common
{
    // Kiểm tra cấu hình bắt buộc khi khởi động: thiếu thì dừng ngay với thông báo rõ ràng, không chạy với giá trị rỗng
    public static class StartupConfiguration
    {
        // Khoá chỉ dùng khi phát triển (appsettings.Development.json), không được dùng ở môi trường thật
        public const string KhoaJwtPhatTrien = "QLKSan-Dev-Secret-Key-Doi-Khi-Trien-Khai-That-2026!";

        public static void Validate(IConfiguration configuration, IHostEnvironment environment)
        {
            if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
                throw new InvalidOperationException(
                    "Thiếu chuỗi kết nối ConnectionStrings:DefaultConnection. Đặt bằng biến môi trường ConnectionStrings__DefaultConnection, "
                    + "User Secrets hoặc appsettings.Development.json khi phát triển.");

            var khoa = configuration[$"{JwtSettings.SectionName}:Key"];
            if (string.IsNullOrWhiteSpace(khoa) || khoa.Length < 32)
                throw new InvalidOperationException("Thiếu Jwt:Key hoặc khoá ngắn hơn 32 ký tự. Đặt bằng biến môi trường Jwt__Key.");
            if (!environment.IsDevelopment() && khoa == KhoaJwtPhatTrien)
                throw new InvalidOperationException("Đang dùng Jwt:Key của môi trường phát triển. Hãy đặt khoá riêng bằng biến môi trường Jwt__Key.");
        }
    }
}
