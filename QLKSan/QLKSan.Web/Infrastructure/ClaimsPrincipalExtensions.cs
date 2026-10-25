using System.Security.Claims;

namespace QLKSan.Infrastructure
{
    // Đọc thông tin người dùng đang đăng nhập (thay cho cookie "Username"/"UserRole" của bản cũ)
    public static class ClaimsPrincipalExtensions
    {
        public static bool IsLoggedIn(this ClaimsPrincipal user)
            => user?.Identity?.IsAuthenticated ?? false;

        // "Admin" | "Receptionist" | "Customer" | null
        public static string GetRole(this ClaimsPrincipal user)
            => user?.FindFirstValue(ClaimTypes.Role);

        public static string GetHoTen(this ClaimsPrincipal user)
            => user?.FindFirstValue(WebClaimTypes.HoTen) ?? user?.Identity?.Name;

        public static string GetAccessToken(this ClaimsPrincipal user)
            => user?.FindFirstValue(WebClaimTypes.AccessToken);

        public static string GetMaNhanVien(this ClaimsPrincipal user)
            => user?.FindFirstValue(WebClaimTypes.MaNhanVien);

        public static string GetMaKhachHang(this ClaimsPrincipal user)
            => user?.FindFirstValue(WebClaimTypes.MaKhachHang);
    }
}
