using System;
using System.Collections.Generic;

namespace QLKSan.Models.DTOs
{
    public class ContactDto
    {
        public string MaLienHe { get; set; }
        public string SDT { get; set; }
        public string Email { get; set; }
        public string DiaChi { get; set; }
        public string QuocGia { get; set; }
    }

    // Thông tin khách rút gọn, dùng khi lồng trong phiếu đặt phòng
    public class CustomerSummaryDto
    {
        public string MaKhachHang { get; set; }
        public string Ho { get; set; }
        public string Ten { get; set; }
        public string HoTen => $"{Ho} {Ten}".Trim();
        public string CCCD_Passport { get; set; }

        // Lấy từ liên hệ đầu tiên của khách
        public string SDT { get; set; }
        public string Email { get; set; }

        // Để lễ tân chuẩn bị đón khách
        public bool LaVIP { get; set; }
        public string SoThich { get; set; }
    }

    public class CustomerDto
    {
        public string MaKhachHang { get; set; }
        public string MaDangNhap { get; set; }
        public string Ho { get; set; }
        public string Ten { get; set; }
        public string HoTen => $"{Ho} {Ten}".Trim();
        public DateTime NgaySinh { get; set; }
        public string CCCD_Passport { get; set; }
        public int DiemTichLuy { get; set; }
        public int DiemDaDoi { get; set; }
        public int SoLanDatPhong { get; set; }

        // Chăm sóc khách. GhiChuNoiBo / HanChe / LyDoHanChe chỉ trả cho nhân viên (API bỏ trống ở hồ sơ của chính khách)
        public bool LaVIP { get; set; }
        public string SoThich { get; set; }
        public string GhiChuNoiBo { get; set; }
        public bool HanChe { get; set; }
        public string LyDoHanChe { get; set; }

        // Hạng thành viên và quyền lợi (API điền ở các endpoint hồ sơ khách)
        public MembershipDto ThanhVien { get; set; }
        public List<ContactDto> LienHeDiaChis { get; set; } = new List<ContactDto>();
    }

    // Một dòng lịch sử điểm: SoDiem dương = cộng, âm = trừ
    public class PointHistoryDto
    {
        public string MaLichSu { get; set; }
        public DateTime ThoiGian { get; set; }
        public int SoDiem { get; set; }
        public string Loai { get; set; }
        public string NoiDung { get; set; }
        public string MaHoaDon { get; set; }
        public string TenNhanVien { get; set; }
    }

    public class MembershipDto
    {
        public string Hang { get; set; }
        public int PhanTramGiamPhong { get; set; }
        public List<string> QuyenLoi { get; set; } = new List<string>();

        // Điểm trọn đời dùng xét hạng (điểm hiện có + điểm đã đổi)
        public int DiemXetHang { get; set; }

        // Hạng kế tiếp (null nếu đã ở hạng cao nhất), số điểm còn thiếu và mốc điểm của hạng đó
        public string HangKeTiep { get; set; }
        public int DiemLenHang { get; set; }
        public int MocHangKeTiep { get; set; }

        // Quy đổi điểm khi thanh toán
        public int GiaTriMotDiem { get; set; }
        public int DiemDoiToiThieu { get; set; }
    }

    // Hồ sơ khách kèm lịch sử đặt phòng
    public class CustomerDetailDto : CustomerDto
    {
        public List<BookingDto> PhieuDatPhongs { get; set; } = new List<BookingDto>();
    }
}
