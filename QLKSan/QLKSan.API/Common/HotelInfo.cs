namespace QLKSan.Common
{
    // Thông tin liên hệ của khách sạn, ánh xạ từ mục "HotelInfo" trong appsettings.json (sửa tại đó khi triển khai thật)
    public class HotelInfo
    {
        public const string SectionName = "HotelInfo";

        public string Ten { get; set; } = "LUXE RESERVE";
        public string MoTaNgan { get; set; }
        public string DiaChi { get; set; }
        public string Hotline { get; set; }
        public string Email { get; set; }

        // Liên kết mở bản đồ (Google Maps…) tới địa chỉ khách sạn
        public string BanDoUrl { get; set; }
    }
}
