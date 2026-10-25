using System;
using System.Collections.Generic;

namespace QLKSan.Models.DTOs
{
    // Quy tắc điều chỉnh giá phòng theo mùa / ngày trong tuần / dịp lễ (bảng QuyTacGia)
    public class PricingRuleDto
    {
        public string MaQuyTac { get; set; }
        public string TenQuyTac { get; set; }

        // null = áp dụng cho mọi loại phòng
        public string MaLoai { get; set; }
        public string TenLoai { get; set; }

        // null = không giới hạn ngày
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }

        // Các đêm trong tuần được áp dụng (0 = Chủ nhật ... 6 = Thứ bảy); rỗng = mọi đêm
        public List<int> CacThu { get; set; } = new List<int>();

        // +20 = tăng 20%, -10 = giảm 10%
        public decimal PhanTramDieuChinh { get; set; }

        // Nhiều quy tắc cùng áp dụng cho 1 đêm thì lấy quy tắc ưu tiên cao nhất
        public int UuTien { get; set; }
        public bool KichHoat { get; set; }
    }

    // Báo giá một khoảng ngày: giá từng đêm sau khi áp dụng quy tắc
    public class PriceQuoteDto
    {
        public string MaPhong { get; set; }
        public decimal GiaGoc { get; set; }
        public int SoDem { get; set; }
        public decimal TongTien { get; set; }
        public decimal GiaTrungBinhDem { get; set; }
        public List<NightPriceDto> CacDem { get; set; } = new List<NightPriceDto>();
    }

    public class NightPriceDto
    {
        public DateTime Dem { get; set; }
        public decimal Gia { get; set; }
        public string QuyTac { get; set; }
        public decimal PhanTramDieuChinh { get; set; }
    }

    // Một phiếu đặt trên lịch phòng (timeline)
    public class TimelineBookingDto
    {
        public string MaDatPhong { get; set; }
        public string MaPhong { get; set; }
        public string HoTenKhachHang { get; set; }
        public DateTime TuNgay { get; set; }

        // Ngày trả (không tính đêm này): thực tế nếu đã trả phòng, dự kiến nếu chưa
        public DateTime DenNgay { get; set; }
        public Enums.BookingStatus TrangThai { get; set; }
    }

    // Một dòng trong sổ khách lưu trú (phục vụ thông báo lưu trú)
    public class GuestRegisterDto
    {
        public string MaDatPhong { get; set; }
        public string HoTen { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string CCCD_Passport { get; set; }
        public string QuocGia { get; set; }
        public string DiaChi { get; set; }
        public string SDT { get; set; }
        public string SoPhong { get; set; }
        public int SoLuongKhach { get; set; }
        public DateTime NgayDen { get; set; }
        public DateTime? NgayDi { get; set; }
        public DateTime NgayDiDuKien { get; set; }
        public Enums.BookingStatus TrangThai { get; set; }

        // Dòng của người đi cùng (khai báo theo phiếu của khách đứng tên)
        public bool LaNguoiDiCung { get; set; }
        public string NguoiDungTen { get; set; }
    }
}
