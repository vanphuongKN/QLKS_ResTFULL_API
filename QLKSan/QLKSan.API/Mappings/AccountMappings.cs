using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;

namespace QLKSan.Mappings
{
    public static class AccountMappings
    {
        // Tài khoản cần được load kèm NhanVien/KhachHang
        public static UserInfoDto ToUserInfoDto(this TaiKhoan account)
        {
            if (account == null) return null;
            var dto = new UserInfoDto();
            account.FillUserInfo(dto);
            return dto;
        }

        public static void FillUserInfo(this TaiKhoan account, UserInfoDto dto)
        {
            dto.MaDangNhap = account.MaDangNhap;
            dto.TenDangNhap = account.TenDangNhap;
            dto.VaiTro = account.VaiTro;
            dto.MaNhanVien = account.NhanVien?.MaNhanVien;
            dto.MaKhachHang = account.KhachHang?.MaKhachHang;
            dto.HoTen = account.NhanVien?.TenNhanVien
                ?? (account.KhachHang != null ? $"{account.KhachHang.Ho} {account.KhachHang.Ten}".Trim() : account.TenDangNhap);
        }
    }
}
