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

        // Tiền cọc khách vãng lai (% tiền phòng dự kiến)
        public int PhanTramTienCocVangLai { get; set; } = 50;

        // Tự động huỷ phiếu chờ duyệt/đã duyệt khi đã qua ngày nhận phòng mà khách không đến
        public bool TuDongHuyKhachKhongDen { get; set; } = true;
        public int ChuKyKiemTraKhongDenPhut { get; set; } = 60;

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
