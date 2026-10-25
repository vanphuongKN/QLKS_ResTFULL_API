namespace QLKSan.Common
{
    // Chính sách khách sạn, ánh xạ từ mục "HotelPolicy" trong appsettings.json
    public class HotelPolicy
    {
        public const string SectionName = "HotelPolicy";

        // Giờ nhận phòng / trả phòng tiêu chuẩn (giờ trong ngày, 0-23)
        public int GioNhanPhong { get; set; } = 14;
        public int GioTraPhong { get; set; } = 12;

        // Phụ thu trả phòng muộn theo mốc giờ (tính theo % giá 1 đêm). Sau mốc cuối cùng: tính thêm 1 đêm (100%)
        // Để trống mặc định: configuration binder NỐI THÊM vào list có sẵn chứ không thay thế
        public List<MucPhuThu> PhuThuTraMuon { get; set; } = new List<MucPhuThu>();

        // Phiếu đặt online được xác nhận ngay khi còn phòng (false: chờ lễ tân duyệt)
        public bool TuDongXacNhanDatPhong { get; set; } = true;

        // Tiền cọc khách vãng lai (% tiền phòng dự kiến)
        public int PhanTramTienCocVangLai { get; set; } = 50;

        // Khách không đến (no-show) được xử lý khi chốt ngày (NightAuditService), không còn tự huỷ định kỳ

        // Chính sách chuẩn (giống appsettings.json) — dùng cho unit test
        public static HotelPolicy MacDinh() => new HotelPolicy
        {
            PhuThuTraMuon = new List<MucPhuThu>
            {
                new MucPhuThu { DenGio = 15, PhanTram = 30 },
                new MucPhuThu { DenGio = 18, PhanTram = 50 }
            }
        };
    }

    public class MucPhuThu
    {
        // Trả phòng đến (bao gồm) giờ này thì áp dụng mức phụ thu
        public int DenGio { get; set; }
        public int PhanTram { get; set; }
    }
}
