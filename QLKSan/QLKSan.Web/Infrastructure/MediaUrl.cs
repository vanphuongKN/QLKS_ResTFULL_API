namespace QLKSan.Infrastructure
{
    // Đường dẫn ảnh hiển thị trên Web: ảnh do quản trị tải lên được lưu ở QLKSan.API (/uploads/...),
    // ảnh có sẵn nằm trong wwwroot của Web (/images/...)
    public static class MediaUrl
    {
        public const string AnhMacDinh = "/images/room_garden.png";
        private static string _apiBase = "";

        // Gọi một lần khi khởi động với ApiSettings:BaseUrl
        public static void Configure(Uri apiBaseUrl) => _apiBase = apiBaseUrl.ToString().TrimEnd('/');

        public static string Of(string duongDan)
        {
            if (string.IsNullOrWhiteSpace(duongDan)) return AnhMacDinh;
            return duongDan.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) ? _apiBase + duongDan : duongDan;
        }
    }
}
