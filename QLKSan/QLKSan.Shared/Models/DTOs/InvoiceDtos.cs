using System;
using System.Collections.Generic;
using QLKSan.Enums;

namespace QLKSan.Models.DTOs
{
    public class InvoiceDto
    {
        public string MaHoaDon { get; set; }
        public string MaDatPhong { get; set; }
        public string MaNhanVienLap { get; set; }
        public DateTime NgayLapHoaDon { get; set; }
        public decimal TongTienPhong { get; set; }
        public decimal TongTienDichVu { get; set; }
        public string MaKhuyenMai { get; set; }
        public decimal GiamGiaKhuyenMai { get; set; }

        // Ưu đãi hạng thành viên (% tiền phòng) và hạng của khách lúc lập hoá đơn
        public decimal GiamGiaThanhVien { get; set; }
        public string HangThanhVien { get; set; }
        public decimal TienPhat { get; set; }
        public decimal ThueVAT { get; set; }
        public decimal TongTienCanThanhToan { get; set; }

        // Tiền cọc đã thu và số tiền còn phải thu (âm = hoàn lại khách)
        public decimal TienCoc { get; set; }
        public decimal SoTienConLai { get; set; }

        // Số đêm tính tiền thực tế (lưu trên hoá đơn) và % phụ thu trả muộn (API tính khi trả về)
        public int SoDem { get; set; }
        public int PhanTramPhuThu { get; set; }

        public InvoiceStatus TrangThai { get; set; }
        public List<PaymentDto> ThanhToans { get; set; } = new List<PaymentDto>();

        // Xuất hoá đơn VAT cho công ty (trống = cá nhân)
        public string TenCongTy { get; set; }
        public string MaSoThue { get; set; }
        public string DiaChiCongTy { get; set; }
        public string EmailHoaDon { get; set; }
    }

    // GET /api/bookings/{id}/folio: bảng kê lưu trú tính tới thời điểm hiện tại (chưa thanh toán) hoặc theo hoá đơn đã thanh toán
    public class FolioDto
    {
        public string MaDatPhong { get; set; }
        public string MaHoaDon { get; set; }
        public InvoiceStatus? TrangThaiHoaDon { get; set; }
        public string HoTenKhachHang { get; set; }
        public string SoPhong { get; set; }
        public string TenLoaiPhong { get; set; }
        public DateTime NgayNhan { get; set; }
        public DateTime NgayTraDuKien { get; set; }
        public DateTime ThoiDiemTinh { get; set; }

        public int SoDem { get; set; }
        public List<NightPriceDto> CacDem { get; set; } = new List<NightPriceDto>();
        public decimal TienPhong { get; set; }
        public int PhanTramPhuThu { get; set; }
        public decimal PhuThuTraMuon { get; set; }
        public List<ServiceUsageDto> DichVu { get; set; } = new List<ServiceUsageDto>();
        public decimal TongTienDichVu { get; set; }
        public string MaKhuyenMai { get; set; }
        public decimal GiamGiaKhuyenMai { get; set; }
        public string HangThanhVien { get; set; }
        public decimal GiamGiaThanhVien { get; set; }
        public decimal TienTruocThue { get; set; }
        public decimal ThueVAT { get; set; }
        public decimal TongCong { get; set; }

        public decimal TienCoc { get; set; }
        public decimal DaThanhToan { get; set; }

        // Còn phải thu (âm = cọc thừa, hoàn lại khách)
        public decimal ConPhaiThu { get; set; }

        public List<DepositDto> GiaoDichCocs { get; set; } = new List<DepositDto>();
        public List<PaymentDto> ThanhToans { get; set; } = new List<PaymentDto>();
        public List<RoomMoveDto> LichSuDoiPhongs { get; set; } = new List<RoomMoveDto>();
    }

    public class PaymentDto
    {
        public string MaThanhToan { get; set; }
        public PaymentType LoaiGiaoDich { get; set; }
        public string MaNhanVien { get; set; }
        public string TenNhanVien { get; set; }
        public string PhuongThuc { get; set; }
        public DateTime NgayThanhToan { get; set; }
        public decimal SoTienDaTra { get; set; }
        public string GhiChu { get; set; }
    }
}
