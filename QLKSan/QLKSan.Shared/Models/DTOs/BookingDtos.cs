using System;
using System.Collections.Generic;
using QLKSan.Enums;

namespace QLKSan.Models.DTOs
{
    // Phiếu đặt rút gọn, dùng khi lồng trong RoomDto
    public class BookingSummaryDto
    {
        public string MaDatPhong { get; set; }
        public DateTime NgayCheckInDuKien { get; set; }
        public DateTime NgayCheckOutDuKien { get; set; }
        public DateTime? NgayCheckInThucTe { get; set; }
        public BookingStatus TrangThai { get; set; }
        public CustomerSummaryDto KhachHang { get; set; }
        public List<ServiceUsageDto> SuDungDichVus { get; set; } = new List<ServiceUsageDto>();
    }

    // Một phòng trong phiếu đặt (ChiTietDatPhong)
    public class BookingRoomDto
    {
        public string MaCTDP { get; set; }
        public string MaPhong { get; set; }
        public RentalType HinhThucThue { get; set; }
        public decimal GiaThueThucTe { get; set; }
        public RoomBriefDto Phong { get; set; }
    }

    // Một lần sử dụng dịch vụ (SuDungDichVu)
    public class ServiceUsageDto
    {
        public string MaSDDV { get; set; }
        public string MaDatPhong { get; set; }
        public string MaDichVu { get; set; }
        public string TenDichVu { get; set; }
        public string DonViTinh { get; set; }
        public int SoLuong { get; set; }
        public DateTime NgaySuDung { get; set; }
        public decimal GiaThucTe { get; set; }
        public decimal ThanhTien { get; set; }
    }

    public class BookingDto
    {
        public string MaDatPhong { get; set; }
        public string MaKhachHang { get; set; }
        public string MaNhanVien { get; set; }
        public DateTime NgayDat { get; set; }
        public DateTime NgayCheckInDuKien { get; set; }
        public DateTime NgayCheckOutDuKien { get; set; }
        public DateTime? NgayCheckInThucTe { get; set; }
        public DateTime? NgayCheckOutThucTe { get; set; }
        public int SoLuongKhach { get; set; }
        public BookingStatus TrangThai { get; set; }
        public decimal TienCoc { get; set; }
        public string GhiChu { get; set; }
        public string MaKhuyenMai { get; set; }

        // Yêu cầu riêng của khách khi đặt online (giường, dịp đặc biệt…) và khung giờ dự kiến đến
        public string YeuCauDacBiet { get; set; }
        public string GioDenDuKien { get; set; }

        // Kênh đặt phòng (Online, Điện thoại, Email, Tại quầy, Đại lý, Vãng lai)
        public string NguonDat { get; set; }

        // Sổ tiền cọc của phiếu (thu / hoàn); TienCoc = tổng thu - tổng hoàn
        public List<DepositDto> GiaoDichCocs { get; set; } = new List<DepositDto>();

        // Người đi cùng (khai báo lưu trú) và lịch sử đổi phòng khi đang ở
        public List<CompanionDto> KhachDiCungs { get; set; } = new List<CompanionDto>();
        public List<RoomMoveDto> LichSuDoiPhongs { get; set; } = new List<RoomMoveDto>();

        public CustomerSummaryDto KhachHang { get; set; }
        public List<BookingRoomDto> ChiTietDatPhongs { get; set; } = new List<BookingRoomDto>();
        public List<ServiceUsageDto> SuDungDichVus { get; set; } = new List<ServiceUsageDto>();
        public InvoiceDto HoaDon { get; set; }
    }

    public class CompanionDto
    {
        public int MaKhachDiCung { get; set; }
        public string HoTen { get; set; }
        public string CCCD_Passport { get; set; }
        public string QuocTich { get; set; }
    }

    public class RoomMoveDto
    {
        public string TuPhong { get; set; }
        public string DenPhong { get; set; }
        public string SoPhongCu { get; set; }
        public string SoPhongMoi { get; set; }
        public DateTime ThoiGian { get; set; }
        public string LyDo { get; set; }
        public decimal GiaCu { get; set; }
        public decimal GiaMoi { get; set; }
        public string TenNhanVien { get; set; }
    }

    // Một lần thu / hoàn tiền cọc
    public class DepositDto
    {
        public int MaGiaoDichCoc { get; set; }
        public PaymentType LoaiGiaoDich { get; set; }
        public decimal SoTien { get; set; }
        public string PhuongThuc { get; set; }
        public DateTime ThoiGian { get; set; }
        public string MaNhanVien { get; set; }
        public string TenNhanVien { get; set; }
        public string GhiChu { get; set; }
    }
}
