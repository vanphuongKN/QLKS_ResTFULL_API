using System;
using System.Collections.Generic;
using QLKSan.Enums;

namespace QLKSan.Models.DTOs
{
    // Một dòng trong danh sách hoá đơn của kế toán
    public class InvoiceListItemDto
    {
        public string MaHoaDon { get; set; }
        public string MaDatPhong { get; set; }
        public string HoTenKhachHang { get; set; }
        public string SoPhong { get; set; }
        public DateTime NgayLapHoaDon { get; set; }
        public decimal TongTienCanThanhToan { get; set; }
        public decimal TienCoc { get; set; }

        // Phần cọc thực trừ vào hoá đơn (không quá tổng hoá đơn) và phần cọc thừa đã hoàn lại khách
        public decimal CocDaTru { get; set; }
        public decimal CocThua { get; set; }
        public decimal DaThu { get; set; }
        public decimal DaHoan { get; set; }
        public InvoiceStatus TrangThai { get; set; }
        public string TenNhanVienLap { get; set; }

        // Các phương thức đã thu (tách thanh toán: nối bằng " + ")
        public string PhuongThuc { get; set; }
    }

    // Chi tiết hoá đơn cho kế toán: hoá đơn + phiếu đặt + các giao dịch thu/hoàn
    public class InvoiceDetailDto
    {
        public InvoiceDto HoaDon { get; set; }
        public BookingDto PhieuDatPhong { get; set; }
        public string TenNhanVienLap { get; set; }
        public decimal DaThu { get; set; }
        public decimal DaHoan { get; set; }
        public decimal CocDaTru { get; set; }
        public decimal CocThua { get; set; }

        // Số tiền tối đa còn có thể hoàn (tổng giá trị hoá đơn − đã hoàn)
        public decimal CoTheHoan { get; set; }
        public List<PaymentDto> GiaoDich { get; set; } = new List<PaymentDto>();
    }

    public class RevenueReportDto
    {
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public int SoHoaDon { get; set; }

        // Tổng giá trị hoá đơn đã thanh toán (gồm VAT và phần đã cọc)
        public decimal TongDoanhThu { get; set; }
        public decimal TongHoanTien { get; set; }
        public decimal DoanhThuThuan { get; set; }

        public decimal TienPhong { get; set; }
        public decimal TienDichVu { get; set; }
        public decimal PhuThu { get; set; }
        public decimal GiamGia { get; set; }
        public decimal GiamGiaThanhVien { get; set; }
        public decimal ThueVAT { get; set; }

        public List<RevenueByDayDto> TheoNgay { get; set; } = new List<RevenueByDayDto>();
        public List<RevenueByRoomTypeDto> TheoLoaiPhong { get; set; } = new List<RevenueByRoomTypeDto>();
        public List<RevenueByServiceDto> TheoDichVu { get; set; } = new List<RevenueByServiceDto>();
        public List<RevenueByMethodDto> TheoPhuongThuc { get; set; } = new List<RevenueByMethodDto>();
        public List<RevenueByStaffDto> TheoNhanVien { get; set; } = new List<RevenueByStaffDto>();
    }

    public class RevenueByDayDto
    {
        public DateTime Ngay { get; set; }
        public int SoHoaDon { get; set; }
        public decimal DoanhThu { get; set; }
        public decimal HoanTien { get; set; }
    }

    public class RevenueByRoomTypeDto
    {
        public string TenLoai { get; set; }
        public int SoLuot { get; set; }
        public int SoDem { get; set; }
        public decimal DoanhThuPhong { get; set; }
    }

    public class RevenueByServiceDto
    {
        public string TenDichVu { get; set; }
        public int SoLuong { get; set; }
        public decimal DoanhThu { get; set; }
    }

    public class RevenueByMethodDto
    {
        public string PhuongThuc { get; set; }
        public int SoGiaoDich { get; set; }
        public decimal SoTien { get; set; }
    }

    // Tiền mỗi nhân viên đã thu (dùng để đối soát/chốt ca)
    public class RevenueByStaffDto
    {
        public string MaNhanVien { get; set; }
        public string TenNhanVien { get; set; }
        public int SoGiaoDich { get; set; }
        public decimal SoTienThu { get; set; }
    }

    public class OccupancyReportDto
    {
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public int TongSoPhong { get; set; }
        public int TongDemPhongDaBan { get; set; }
        public decimal TyLeTrungBinh { get; set; }
        public List<OccupancyByDayDto> TheoNgay { get; set; } = new List<OccupancyByDayDto>();
    }

    public class OccupancyByDayDto
    {
        public DateTime Ngay { get; set; }
        public int SoPhongCoKhach { get; set; }
        public decimal TyLe { get; set; }
    }
}
