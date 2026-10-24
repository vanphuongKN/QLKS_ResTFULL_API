using System.ComponentModel.DataAnnotations;
using QLKSan.Enums;

namespace QLKSan.Models.Requests
{
    public class UpdateStaffRequest
    {
        [Required(ErrorMessage = "Tên nhân viên không được để trống")]
        [StringLength(100)]
        public string TenNhanVien { get; set; }

        [StringLength(50)]
        public string ChucVu { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})\b", ErrorMessage = "Số điện thoại nhân viên không hợp lệ.")]
        public string SDT { get; set; }

        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Email sai định dạng")]
        public string Email { get; set; }

        // Đổi vai trò tài khoản / trạng thái làm việc (để trống = giữ nguyên)
        public Role? VaiTro { get; set; }
        public EmployeeStatus? TrangThai { get; set; }
    }

    // Thêm nhân viên kèm tạo tài khoản đăng nhập
    public class CreateStaffRequest : UpdateStaffRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(50)]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [MinLength(6, ErrorMessage = "Mật khẩu nhân viên phải từ 6 ký tự trở lên.")]
        public string MatKhau { get; set; }
    }

    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải từ 6 ký tự trở lên.")]
        public string MatKhauMoi { get; set; }
    }
}
