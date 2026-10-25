using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;

namespace QLKSan.Mappings
{
    public static class HousekeepingMappings
    {
        public static HousekeepingTaskDto ToDto(this PhanCongDonPhong entity)
        {
            if (entity == null) return null;
            return new HousekeepingTaskDto
            {
                MaPhanCong = entity.MaPhanCong,
                MaPhong = entity.MaPhong,
                SoPhong = entity.Phong?.SoPhong,
                TenLoaiPhong = entity.Phong?.LoaiPhong?.TenLoai,
                MaNhanVien = entity.MaNhanVien,
                TenNhanVien = entity.NhanVien?.TenNhanVien,
                MaDatPhong = entity.MaDatPhong,
                LoaiCongViec = entity.LoaiCongViec,
                TrangThai = entity.TrangThai,
                ThoiGianTao = entity.ThoiGianTao,
                ThoiGianGiao = entity.ThoiGianGiao,
                ThoiGianBatDau = entity.ThoiGianBatDau,
                ThoiGianHoanThanh = entity.ThoiGianHoanThanh,
                GhiChu = entity.GhiChu,
                Tang = Business.HousekeepingTasks.TangCua(entity.Phong?.SoPhong),
                MaYeuCau = entity.MaYeuCau,
                ThoiGianKiemTra = entity.ThoiGianKiemTra,
                TenNguoiKiemTra = entity.NhanVienKiemTra?.TenNhanVien,
                KetQuaKiemTra = entity.KetQuaKiemTra,
                SoLanKhongDat = entity.SoLanKhongDat
            };
        }

        public static MaintenanceTicketDto ToDto(this PhieuKyThuat entity)
        {
            if (entity == null) return null;
            return new MaintenanceTicketDto
            {
                MaPhieu = entity.MaPhieu,
                MaPhong = entity.MaPhong,
                SoPhong = entity.Phong?.SoPhong,
                MoTa = entity.MoTa,
                MucDo = entity.MucDo,
                KhoaPhong = entity.KhoaPhong,
                TrangThai = entity.TrangThai,
                TenNguoiBao = entity.NhanVienBao?.TenNhanVien,
                ThoiGianBao = entity.ThoiGianBao,
                TenNguoiXuLy = entity.NhanVienXuLy?.TenNhanVien,
                ThoiGianXong = entity.ThoiGianXong,
                KetQua = entity.KetQua,
                MaPhanCong = entity.MaPhanCong,
                MaYeuCau = entity.MaYeuCau
            };
        }
    }
}
