using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;

namespace QLKSan.Mappings
{
    public static class BookingMappings
    {
        public static BookingSummaryDto ToSummaryDto(this PhieuDatPhong entity)
        {
            if (entity == null) return null;
            return new BookingSummaryDto
            {
                MaDatPhong = entity.MaDatPhong,
                NgayCheckInDuKien = entity.NgayCheckInDuKien,
                NgayCheckOutDuKien = entity.NgayCheckOutDuKien,
                NgayCheckInThucTe = entity.NgayCheckInThucTe,
                TrangThai = entity.TrangThai,
                KhachHang = entity.KhachHang.ToSummaryDto(),
                SuDungDichVus = entity.SuDungDichVus?.Select(s => s.ToDto()).ToList() ?? new List<ServiceUsageDto>()
            };
        }

        public static BookingDto ToDto(this PhieuDatPhong entity)
        {
            if (entity == null) return null;
            var dto = MapBooking(entity);

            // Số đêm lưu trên hoá đơn (hoá đơn cũ chưa có thì suy ra từ TongTienPhong = giá × số đêm); % phụ thu = TienPhat / giá gốc
            var giaMotDem = entity.ChiTietDatPhongs?.FirstOrDefault()?.GiaThueThucTe ?? 0;
            if (dto.HoaDon != null && giaMotDem > 0)
            {
                dto.HoaDon.SoDem = entity.HoaDon.SoDem > 0 ? entity.HoaDon.SoDem : (int)Math.Round(dto.HoaDon.TongTienPhong / giaMotDem);
                dto.HoaDon.PhanTramPhuThu = (int)Math.Round(dto.HoaDon.TienPhat * 100 / giaMotDem);
            }
            return dto;
        }

        private static BookingDto MapBooking(PhieuDatPhong entity)
        {
            return new BookingDto
            {
                MaDatPhong = entity.MaDatPhong,
                MaKhachHang = entity.MaKhachHang,
                MaNhanVien = entity.MaNhanVien,
                NgayDat = entity.NgayDat,
                NgayCheckInDuKien = entity.NgayCheckInDuKien,
                NgayCheckOutDuKien = entity.NgayCheckOutDuKien,
                NgayCheckInThucTe = entity.NgayCheckInThucTe,
                NgayCheckOutThucTe = entity.NgayCheckOutThucTe,
                SoLuongKhach = entity.SoLuongKhach,
                TrangThai = entity.TrangThai,
                TienCoc = entity.TienCoc,
                GhiChu = entity.GhiChu,
                MaKhuyenMai = entity.MaKhuyenMai,
                YeuCauDacBiet = entity.YeuCauDacBiet,
                GioDenDuKien = entity.GioDenDuKien,
                KhachHang = entity.KhachHang.ToSummaryDto(),
                ChiTietDatPhongs = entity.ChiTietDatPhongs?.Select(c => c.ToDto()).ToList() ?? new List<BookingRoomDto>(),
                SuDungDichVus = entity.SuDungDichVus?.Select(s => s.ToDto()).ToList() ?? new List<ServiceUsageDto>(),
                HoaDon = entity.HoaDon.ToDto()
            };
        }

        public static BookingRoomDto ToDto(this ChiTietDatPhong entity)
        {
            if (entity == null) return null;
            return new BookingRoomDto
            {
                MaCTDP = entity.MaCTDP,
                MaPhong = entity.MaPhong,
                HinhThucThue = entity.HinhThucThue,
                GiaThueThucTe = entity.GiaThueThucTe,
                Phong = entity.Phong.ToBriefDto()
            };
        }

        public static ServiceUsageDto ToDto(this SuDungDichVu entity)
        {
            if (entity == null) return null;
            return new ServiceUsageDto
            {
                MaSDDV = entity.MaSDDV,
                MaDatPhong = entity.MaDatPhong,
                MaDichVu = entity.MaDichVu,
                TenDichVu = entity.DichVu?.TenDichVu,
                DonViTinh = entity.DichVu?.DonViTinh,
                SoLuong = entity.SoLuong,
                NgaySuDung = entity.NgaySuDung,
                GiaThucTe = entity.GiaThucTe,
                ThanhTien = entity.ThanhTien
            };
        }

        public static InvoiceDto ToDto(this HoaDon entity)
        {
            if (entity == null) return null;
            return new InvoiceDto
            {
                MaHoaDon = entity.MaHoaDon,
                MaDatPhong = entity.MaDatPhong,
                MaNhanVienLap = entity.MaNhanVienLap,
                NgayLapHoaDon = entity.NgayLapHoaDon,
                TongTienPhong = entity.TongTienPhong,
                TongTienDichVu = entity.TongTienDichVu,
                MaKhuyenMai = entity.MaKhuyenMai,
                GiamGiaKhuyenMai = entity.GiamGiaKhuyenMai,
                GiamGiaThanhVien = entity.GiamGiaThanhVien,
                HangThanhVien = entity.HangThanhVien,
                TienPhat = entity.TienPhat,
                ThueVAT = entity.ThueVAT,
                TongTienCanThanhToan = entity.TongTienCanThanhToan,
                TienCoc = entity.TienCoc,
                SoTienConLai = entity.SoTienConLai,
                TrangThai = entity.TrangThai,
                ThanhToans = entity.ThanhToans?.Select(t => t.ToDto()).ToList() ?? new List<PaymentDto>()
            };
        }

        public static PaymentDto ToDto(this ThanhToan entity)
        {
            if (entity == null) return null;
            return new PaymentDto
            {
                MaThanhToan = entity.MaThanhToan,
                LoaiGiaoDich = entity.LoaiGiaoDich,
                MaNhanVien = entity.MaNhanVien,
                PhuongThuc = entity.PhuongThuc,
                NgayThanhToan = entity.NgayThanhToan,
                SoTienDaTra = entity.SoTienDaTra,
                GhiChu = entity.GhiChu
            };
        }
    }
}
