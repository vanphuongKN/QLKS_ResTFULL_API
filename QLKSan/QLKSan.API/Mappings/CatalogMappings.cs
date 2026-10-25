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
                TrangThai = entity.TrangThai,
                LaTraiNghiem = entity.LaTraiNghiem,
                LaMinibar = entity.LaMinibar,
                MoTa = entity.MoTa
            };
        }

        public static DichVu ToEntity(this ServiceRequest request, string maDichVu)
        {
            return new DichVu
            {
                MaDichVu = maDichVu,
                TenDichVu = request.TenDichVu,
                GiaDichVu = request.GiaDichVu,
                DonViTinh = request.DonViTinh,
                LaTraiNghiem = request.LaTraiNghiem,
                LaMinibar = request.LaMinibar,
                MoTa = request.MoTa
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
                TrangThai = entity.TrangThai,
                KichHoat = entity.KichHoat,
                SoLuotToiDa = entity.SoLuotToiDa,
                SoDemToiThieu = entity.SoDemToiThieu,
                MaLoai = entity.MaLoai,
                TenLoai = entity.LoaiPhong?.TenLoai
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
                AnDanh = entity.AnDanh,
                MaDatPhong = entity.MaDatPhong,
                TenLoaiPhong = entity.Phong?.LoaiPhong?.TenLoai,
                // Công khai: ẩn danh thì không lộ tên; còn lại chỉ hiện tên (không kèm họ)
                TenHienThi = entity.AnDanh ? "Khách ẩn danh" : (entity.KhachHang?.Ten ?? "Khách lưu trú"),
                PhanHoi = entity.PhanHoi,
                NgayPhanHoi = entity.NgayPhanHoi,
                TenNhanVienPhanHoi = entity.NhanVienPhanHoi?.TenNhanVien,
                An = entity.An
            };
        }
    }
}
