using System;
using QLKSan.Enums;

namespace QLKSan.Models.DTOs
{
    // Thông tin người dùng đang đăng nhập (GET /api/auth/me)
    public class UserInfoDto
    {
        public string MaDangNhap { get; set; }
        public string TenDangNhap { get; set; }
        public Role VaiTro { get; set; }
        public string HoTen { get; set; }

        // Có giá trị khi tài khoản là nhân viên (Admin/Lễ tân)
        public string MaNhanVien { get; set; }

        // Có giá trị khi tài khoản là khách hàng
        public string MaKhachHang { get; set; }
    }

    // Kết quả đăng nhập/đăng ký: JWT + thông tin người dùng
    public class LoginResponse : UserInfoDto
    {
        public string AccessToken { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
