using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    public interface ITokenService
    {
        // Tạo JWT cho tài khoản (tài khoản cần được load kèm NhanVien/KhachHang)
        LoginResponse CreateToken(TaiKhoan account);
    }
}
