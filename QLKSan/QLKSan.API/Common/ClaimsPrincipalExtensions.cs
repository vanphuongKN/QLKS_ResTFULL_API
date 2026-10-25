using System.Linq;
using System.Security.Claims;

namespace QLKSan.Common
{
    // Đọc thông tin người dùng hiện tại từ JWT (thay cho cookie "Username" ở bản MVC cũ)
    public static class ClaimsPrincipalExtensions
    {
        // Có ít nhất một vai trò trong nhóm quyền dạng "A,B,C" (vd: AppRoles.RoomInspectors)
        public static bool IsInAnyRole(this ClaimsPrincipal user, string roles)
            => roles.Split(',').Any(user.IsInRole);

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
