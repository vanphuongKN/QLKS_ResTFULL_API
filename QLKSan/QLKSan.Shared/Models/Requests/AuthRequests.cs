using System;
using System.ComponentModel.DataAnnotations;

namespace QLKSan.Models.Requests
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        public string MatKhau { get; set; }
    }

    public class RegisterRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập họ.")]
        [StringLength(50)]
        public string Ho { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên.")]
        [StringLength(50)]
        public string Ten { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})\b", ErrorMessage = "Số điện thoại không hợp lệ (gồm 10 số, bắt đầu bằng 03, 05, 07, 08, 09).")]
        public string SDT { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập CCCD/Passport.")]
        [RegularExpression(@"^[0-9]{9,12}$", ErrorMessage = "CCCD/Passport phải bao gồm 9 đến 12 chữ số.")]
        public string CCCD_Passport { get; set; }

        [DataType(DataType.Date)]
        public DateTime NgaySinh { get; set; }

        [EmailAddress(ErrorMessage = "Email sai định dạng.")]
        [StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
        [StringLength(255)]
        public string DiaChi { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(50)]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        public string MatKhau { get; set; }

        [Compare(nameof(MatKhau), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string XacNhanMatKhau { get; set; }
    }
}
