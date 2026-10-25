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
                NguonDat = entity.NguonDat,
                GiaoDichCocs = entity.GiaoDichCocs?.OrderBy(g => g.ThoiGian).Select(g => g.ToDto()).ToList() ?? new List<DepositDto>(),
                KhachDiCungs = entity.KhachDiCungs?.OrderBy(k => k.MaKhachDiCung).Select(k => k.ToDto()).ToList() ?? new List<CompanionDto>(),
                LichSuDoiPhongs = entity.LichSuDoiPhongs?.OrderBy(l => l.ThoiGian).Select(l => new RoomMoveDto
                {
                    TuPhong = l.TuPhong,
                    DenPhong = l.DenPhong,
                    SoPhongCu = l.PhongCu?.SoPhong,
                    SoPhongMoi = l.PhongMoi?.SoPhong,
                    ThoiGian = l.ThoiGian,
                    LyDo = l.LyDo,
                    GiaCu = l.GiaCu,
                    GiaMoi = l.GiaMoi,
                    TenNhanVien = l.NhanVien?.TenNhanVien
                }).ToList() ?? new List<RoomMoveDto>(),
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
                ThanhToans = entity.ThanhToans?.Select(t => t.ToDto()).ToList() ?? new List<PaymentDto>(),
                TenCongTy = entity.TenCongTy,
                MaSoThue = entity.MaSoThue,
                DiaChiCongTy = entity.DiaChiCongTy,
                EmailHoaDon = entity.EmailHoaDon
            };
        }

        public static GuestRequestDto ToDto(this YeuCauKhach entity)
        {
            if (entity == null) return null;
            var mo = entity.TrangThai is Enums.GuestRequestStatus.New or Enums.GuestRequestStatus.InProgress;
            var khach = entity.PhieuDatPhong?.KhachHang;
            return new GuestRequestDto
            {
                MaYeuCau = entity.MaYeuCau,
                MaDatPhong = entity.MaDatPhong,
                MaPhong = entity.MaPhong,
                SoPhong = entity.Phong?.SoPhong,
                HoTenKhachHang = khach == null ? null : $"{khach.Ho} {khach.Ten}".Trim(),
                Loai = entity.Loai,
                NoiDung = entity.NoiDung,
                ThoiGianHen = entity.ThoiGianHen,
                TrangThai = entity.TrangThai,
                ThoiGianTao = entity.ThoiGianTao,
                TenNhanVienTao = entity.NhanVienTao?.TenNhanVien,
                ThoiGianXuLy = entity.ThoiGianXuLy,
                TenNhanVienXuLy = entity.NhanVienXuLy?.TenNhanVien,
                KetQua = entity.KetQua,
                QuaHen = mo && entity.ThoiGianHen != null && entity.ThoiGianHen < DateTime.Now
            };
        }

        public static CompanionDto ToDto(this KhachDiCung entity)
        {
            if (entity == null) return null;
            return new CompanionDto
            {
                MaKhachDiCung = entity.MaKhachDiCung,
                HoTen = entity.HoTen,
                CCCD_Passport = entity.CCCD_Passport,
                QuocTich = entity.QuocTich
            };
        }

        public static DepositDto ToDto(this GiaoDichCoc entity)
        {
            if (entity == null) return null;
            return new DepositDto
            {
                MaGiaoDichCoc = entity.MaGiaoDichCoc,
                LoaiGiaoDich = entity.LoaiGiaoDich,
                SoTien = entity.SoTien,
                PhuongThuc = entity.PhuongThuc,
                ThoiGian = entity.ThoiGian,
                MaNhanVien = entity.MaNhanVien,
                TenNhanVien = entity.NhanVien?.TenNhanVien,
                GhiChu = entity.GhiChu
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
                TenNhanVien = entity.NhanVien?.TenNhanVien,
                PhuongThuc = entity.PhuongThuc,
                NgayThanhToan = entity.NgayThanhToan,
                SoTienDaTra = entity.SoTienDaTra,
                GhiChu = entity.GhiChu
            };
        }
    }
}
