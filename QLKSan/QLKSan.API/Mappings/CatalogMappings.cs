using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;
using QLKSan.Models.Requests;

namespace QLKSan.Mappings
{
    // Dịch vụ, khuyến mãi, nhân viên, đánh giá
    public static class CatalogMappings
    {
        public static ServiceDto ToDto(this DichVu entity)
        {
            if (entity == null) return null;
            return new ServiceDto
            {
                MaDichVu = entity.MaDichVu,
                TenDichVu = entity.TenDichVu,
                GiaDichVu = entity.GiaDichVu,
                DonViTinh = entity.DonViTinh,
                TrangThai = entity.TrangThai
            };
        }

        public static DichVu ToEntity(this ServiceRequest request, string maDichVu)
        {
            return new DichVu
            {
                MaDichVu = maDichVu,
                TenDichVu = request.TenDichVu,
                GiaDichVu = request.GiaDichVu,
                DonViTinh = request.DonViTinh
            };
        }

        public static PromotionDto ToDto(this KhuyenMai entity)
        {
            if (entity == null) return null;
            return new PromotionDto
            {
                MaKhuyenMai = entity.MaKhuyenMai,
                TenKhuyenMai = entity.TenKhuyenMai,
                LoaiGiam = entity.LoaiGiam,
                GiaTriGiam = entity.GiaTriGiam,
                NgayBatDau = entity.NgayBatDau,
                NgayKetThuc = entity.NgayKetThuc,
                TrangThai = entity.TrangThai
            };
        }

        public static KhuyenMai ToEntity(this PromotionRequest request)
        {
            return new KhuyenMai
            {
                MaKhuyenMai = request.MaKhuyenMai,
                TenKhuyenMai = request.TenKhuyenMai,
                LoaiGiam = request.LoaiGiam,
                GiaTriGiam = request.GiaTriGiam,
                NgayBatDau = request.NgayBatDau,
                NgayKetThuc = request.NgayKetThuc
            };
        }

        public static StaffDto ToDto(this NhanVien entity)
        {
            if (entity == null) return null;
            return new StaffDto
            {
                MaNhanVien = entity.MaNhanVien,
                MaDangNhap = entity.MaDangNhap,
                TenDangNhap = entity.TaiKhoan?.TenDangNhap,
                TenNhanVien = entity.TenNhanVien,
                ChucVu = entity.ChucVu,
                SDT = entity.SDT,
                Email = entity.Email,
                NgayVaoLam = entity.NgayVaoLam,
                TrangThai = entity.TrangThai,
                VaiTro = entity.TaiKhoan?.VaiTro
            };
        }

        public static NhanVien ToEntity(this UpdateStaffRequest request, string maNhanVien)
        {
            return new NhanVien
            {
                MaNhanVien = maNhanVien,
                TenNhanVien = request.TenNhanVien,
                ChucVu = request.ChucVu,
                SDT = request.SDT,
                Email = request.Email
            };
        }

        public static ReviewDto ToDto(this DanhGia entity)
        {
            if (entity == null) return null;
            return new ReviewDto
            {
                MaDanhGia = entity.MaDanhGia,
                MaKhachHang = entity.MaKhachHang,
                HoTenKhachHang = entity.KhachHang == null ? null : $"{entity.KhachHang.Ho} {entity.KhachHang.Ten}".Trim(),
                MaPhong = entity.MaPhong,
                SoPhong = entity.Phong?.SoPhong,
                SoSao = entity.SoSao,
                NoiDung = entity.NoiDung,
                NgayDanhGia = entity.NgayDanhGia,
                AnDanh = entity.AnDanh
            };
        }
    }
}
