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
                MaYeuCau = entity.MaYeuCau
            };
        }
    }
}
