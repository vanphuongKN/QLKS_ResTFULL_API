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
    }

    public class PaymentDto
    {
        public string MaThanhToan { get; set; }
        public PaymentType LoaiGiaoDich { get; set; }
        public string MaNhanVien { get; set; }
        public string PhuongThuc { get; set; }
        public DateTime NgayThanhToan { get; set; }
        public decimal SoTienDaTra { get; set; }
        public string GhiChu { get; set; }
    }
}
