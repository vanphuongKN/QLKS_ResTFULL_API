namespace QLKSan.Common
{
    // Chương trình thành viên, ánh xạ từ mục "Loyalty" trong appsettings.json.
    // Hạng xét theo điểm tích luỹ trọn đời (số dư hiện có + điểm đã đổi), nên đổi điểm không làm tụt hạng.
    public class LoyaltyPolicy
    {
        public const string SectionName = "Loyalty";

        // Phương thức ghi trên khoản thanh toán bằng điểm (không hoàn thành tiền mặt)
        public const string PhuongThucDiem = "Điểm thưởng";

        // Tích 1 điểm cho mỗi SoTienMotDiem đồng khách thực trả (không tính phần trả bằng điểm)
        public int SoTienMotDiem { get; set; } = 10000;

        // Giá trị 1 điểm khi đổi để trừ vào hoá đơn
        public int GiaTriMotDiem { get; set; } = 1000;

        // Số điểm tối thiểu mỗi lần đổi
        public int DiemDoiToiThieu { get; set; } = 100;

        // Các hạng theo điểm tối thiểu tăng dần. Để trống mặc định: configuration binder NỐI THÊM vào list có sẵn
        public List<HangThanhVien> Hangs { get; set; } = new List<HangThanhVien>();

        public HangThanhVien HangTheoDiem(int diemXetHang)
            => Hangs.Where(h => h.DiemToiThieu <= diemXetHang).OrderByDescending(h => h.DiemToiThieu).FirstOrDefault()
               ?? new HangThanhVien { Ten = "Thành viên" };

        // Hạng kế tiếp (null nếu đã ở hạng cao nhất)
        public HangThanhVien HangKeTiep(int diemXetHang)
            => Hangs.Where(h => h.DiemToiThieu > diemXetHang).OrderBy(h => h.DiemToiThieu).FirstOrDefault();

        // Chương trình chuẩn (giống appsettings.json) — dùng cho unit test
        public static LoyaltyPolicy MacDinh() => new LoyaltyPolicy
        {
            Hangs = new List<HangThanhVien>
            {
                new HangThanhVien { Ten = "Thành viên Đồng", DiemToiThieu = 0 },
                new HangThanhVien { Ten = "Thành viên Bạc", DiemToiThieu = 1000, PhanTramGiamPhong = 3 },
                new HangThanhVien { Ten = "Thành viên Vàng", DiemToiThieu = 3000, PhanTramGiamPhong = 5 },
                new HangThanhVien { Ten = "Thành viên Kim Cương", DiemToiThieu = 5000, PhanTramGiamPhong = 10 }
            }
        };
    }

    public class HangThanhVien
    {
        public string Ten { get; set; }
        public int DiemToiThieu { get; set; }

        // Giảm % tiền phòng, tự áp dụng khi lập hoá đơn
        public int PhanTramGiamPhong { get; set; }

        // Quyền lợi khác (hiển thị cho khách, lễ tân phục vụ theo tình trạng thực tế)
        public List<string> QuyenLoi { get; set; } = new List<string>();
    }
}
