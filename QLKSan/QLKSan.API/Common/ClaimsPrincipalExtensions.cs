using System.Security.Claims;

namespace QLKSan.Common
{
    // Đọc thông tin người dùng hiện tại từ JWT (thay cho cookie "Username" ở bản MVC cũ)
    public static class ClaimsPrincipalExtensions
    {
        public static string GetMaDangNhap(this ClaimsPrincipal user)
            => user.FindFirstValue(AppClaimTypes.MaDangNhap);

        public static string GetMaNhanVien(this ClaimsPrincipal user)
        {
            var maNhanVien = user.FindFirstValue(AppClaimTypes.MaNhanVien);
            if (string.IsNullOrEmpty(maNhanVien))
                throw new BusinessException("Tài khoản hiện tại không gắn với nhân viên nào.");
            return maNhanVien;
        }

        public static string GetMaKhachHang(this ClaimsPrincipal user)
        {
            var maKhachHang = user.FindFirstValue(AppClaimTypes.MaKhachHang);
            if (string.IsNullOrEmpty(maKhachHang))
                throw new BusinessException("Tài khoản hiện tại không gắn với khách hàng nào.");
            return maKhachHang;
        }
    }
}
