using System.Threading.Tasks;
using QLKSan.Models.Entities;
using QLKSan.Models.Requests;

namespace QLKSan.Business
{
    public interface IAuthService
    {
        // Trả về tài khoản (kèm NhanVien/KhachHang) nếu đúng thông tin, ngược lại null
        Task<TaiKhoan> LoginAsync(string tenDangNhap, string matKhau);

        // Tạo TaiKhoan + KhachHang + LienHe_DiaChi cho khách đăng ký mới
        Task<TaiKhoan> RegisterCustomerAsync(RegisterRequest request);

        Task<TaiKhoan> GetAccountAsync(string maDangNhap);

        // Ném BusinessException nếu mật khẩu hiện tại không đúng
        Task ChangePasswordAsync(string maDangNhap, string matKhauCu, string matKhauMoi);
    }
}
