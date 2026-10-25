using System;
using System.Collections.Generic;

namespace QLKSan.Models.DTOs
{
    // GET /api/night-audit/status: tình trạng chốt ngày (cảnh báo ngày trước chưa chốt)
    public class NightAuditStatusDto
    {
        // Ngày bắt đầu áp dụng chốt ngày (cấu hình NightAudit trong appsettings.json)
        public DateTime NgayBatDau { get; set; }
        public DateTime? NgayChotGanNhat { get; set; }

        // Ngày kế tiếp phải chốt (chốt lần lượt); null nếu đã chốt tới hôm qua
        public DateTime? NgayCanChot { get; set; }

        // Số ngày đã qua (trước hôm nay) chưa chốt
        public int SoNgayChuaChot { get; set; }
    }

    // Số liệu một ngày: lúc xem trước và lúc đã chốt (ảnh chụp lưu lại)
    public class NightAuditFiguresDto
    {
        public DateTime Ngay { get; set; }
        public int TongSoPhong { get; set; }
        public int DemPhongBan { get; set; }
        public decimal CongSuat { get; set; }

        // Doanh thu phòng ghi nhận theo đêm (giá đêm theo quy tắc giá, trước giảm giá, chưa VAT)
        public decimal TienPhong { get; set; }
        public decimal ADR { get; set; }
        public decimal RevPAR { get; set; }

        // Doanh thu khác: cọc giữ lại khi khách huỷ / không đến, ghi nhận ở lần chốt sau khi huỷ
        public decimal DoanhThuKhac { get; set; }

        // Dịch vụ phát sinh trong ngày; hoá đơn thanh toán trong ngày (theo ngày thanh toán)
        public decimal TienDichVu { get; set; }
        public int SoHoaDonThanhToan { get; set; }
        public decimal TongHoaDonThanhToan { get; set; }

        public int KhachDen { get; set; }
        public int KhachDi { get; set; }
        public int KhachKhongDen { get; set; }

        // Tiền thực thu / chi trong ngày theo phương thức (thanh toán, hoàn tiền, thu / hoàn cọc; không tính điểm thưởng)
        public List<ShiftMethodDto> TheoPhuongThuc { get; set; } = new List<ShiftMethodDto>();
        public List<NightRoomRevenueDto> CacDem { get; set; } = new List<NightRoomRevenueDto>();
        public List<DepositHoldingDto> CocGiuLai { get; set; } = new List<DepositHoldingDto>();
    }

    public class NightRoomRevenueDto
    {
        public string MaDatPhong { get; set; }
        public string MaPhong { get; set; }
        public string SoPhong { get; set; }
        public string HoTenKhachHang { get; set; }
        public decimal Gia { get; set; }
        public string QuyTac { get; set; }
    }

    // Phiếu đến tới ngày chốt mà chưa nhận phòng
    public class NoShowCandidateDto
    {
        public string MaDatPhong { get; set; }
        public string HoTenKhachHang { get; set; }
        public string SoPhong { get; set; }
        public DateTime NgayNhan { get; set; }
        public decimal TienCoc { get; set; }
        public string GioDenDuKien { get; set; }
    }

    // GET /api/night-audit/preview: xem trước trước khi chốt
    public class NightAuditPreviewDto
    {
        public DateTime Ngay { get; set; }
        public bool ChoPhepChot { get; set; }
        public string LyDoKhongChot { get; set; }
        public NightAuditStatusDto TinhTrang { get; set; }
        public NightAuditFiguresDto SoLieu { get; set; }

        // Mặc định huỷ + giữ cọc khi chốt; lễ tân bỏ đánh dấu khách báo đến muộn
        public List<NoShowCandidateDto> KhachKhongDen { get; set; } = new List<NoShowCandidateDto>();

        // Cảnh báo (không chặn chốt)
        public List<string> QuaHanTra { get; set; } = new List<string>();
        public int HoaDonChuaThanhToan { get; set; }
        public List<string> CaChuaChot { get; set; } = new List<string>();
    }

    // Một ngày đã chốt
    public class DayCloseDto
    {
        public DateTime Ngay { get; set; }
        public DateTime ThoiGianChot { get; set; }
        public string TenNhanVien { get; set; }
        public string GhiChu { get; set; }
        public NightAuditFiguresDto SoLieu { get; set; }
    }
}
