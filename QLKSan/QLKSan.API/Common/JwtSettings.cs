namespace QLKSan.Common
{
    // Ánh xạ từ mục "Jwt" trong appsettings.json
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Key { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int ExpiresMinutes { get; set; } = 120;
    }
}
